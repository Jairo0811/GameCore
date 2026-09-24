using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameCore.Domain.Entities;
using GameCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GameCore.WebAPI;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapGameCoreApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.MapPost("/auth/login", LoginAsync);

        var secured = api.MapGroup(string.Empty).RequireAuthorization();
        secured.MapGet("/games", GetGamesAsync);
        secured.MapPost("/games", CreateGameAsync);
        secured.MapPut("/games/{id:int}", UpdateGameAsync);
        secured.MapDelete("/games/{id:int}", DeactivateGameAsync);
        secured.MapGet("/customers", GetCustomersAsync);
        secured.MapPost("/customers", CreateCustomerAsync);
        secured.MapPut("/customers/{id:int}", UpdateCustomerAsync);
        secured.MapGet("/sales", GetSalesAsync);
        secured.MapPost("/sales", CreateSaleAsync);
        secured.MapGet("/employees", GetEmployeesAsync);
        secured.MapPost("/employees", CreateEmployeeAsync);
        secured.MapGet("/distributions", GetDistributionsAsync);
        secured.MapPost("/distributions", UpsertDistributionAsync);
        secured.MapGet("/catalogs", GetCatalogsAsync);
        secured.MapGet("/dashboard", GetDashboardAsync);
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, GameCoreDbContext db, IConfiguration configuration)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.AppUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return Results.Unauthorized();

        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = configuration["Jwt:Issuer"] ?? "GameCore";
        var audience = configuration["Jwt:Audience"] ?? "GameCore.Client";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Email, user.Role));
    }

    private static async Task<IResult> GetGamesAsync(GameCoreDbContext db)
    {
        var rows = await db.Games.AsNoTracking()
            .Include(x => x.AgeRating)
            .Include(x => x.GameGenres).ThenInclude(x => x.Genre)
            .Include(x => x.GamePlatforms).ThenInclude(x => x.Platform)
            .OrderBy(x => x.Title)
            .Select(x => new
            {
                x.GameId, x.Title, x.Story, x.ReleaseDate, x.UnitPrice, x.IsActive,
                AgeRating = x.AgeRating == null ? null : x.AgeRating.Code,
                Genres = x.GameGenres.Select(g => g.Genre.Name).ToArray(),
                Platforms = x.GamePlatforms.Select(p => p.Platform.Name).ToArray()
            }).ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateGameAsync(GameWriteRequest request, GameCoreDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.UnitPrice < 0)
            return Results.BadRequest(new { error = "Title is required and unitPrice must be non-negative." });

        var game = new Game
        {
            Title = request.Title.Trim(),
            Story = request.Story?.Trim(),
            ReleaseDate = request.ReleaseDate,
            UnitPrice = request.UnitPrice,
            AgeRatingId = request.AgeRatingId,
            IsActive = true
        };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        await ReplaceGameRelationsAsync(db, game.GameId, request.GenreIds, request.PlatformIds);
        return Results.Created($"/api/games/{game.GameId}", new { game.GameId });
    }

    private static async Task<IResult> UpdateGameAsync(int id, GameWriteRequest request, GameCoreDbContext db)
    {
        var game = await db.Games.SingleOrDefaultAsync(x => x.GameId == id);
        if (game is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(request.Title) || request.UnitPrice < 0)
            return Results.BadRequest(new { error = "Invalid game data." });

        game.Title = request.Title.Trim();
        game.Story = request.Story?.Trim();
        game.ReleaseDate = request.ReleaseDate;
        game.UnitPrice = request.UnitPrice;
        game.AgeRatingId = request.AgeRatingId;
        game.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        await ReplaceGameRelationsAsync(db, id, request.GenreIds, request.PlatformIds);
        return Results.NoContent();
    }

    private static async Task ReplaceGameRelationsAsync(GameCoreDbContext db, int gameId, int[] genreIds, int[] platformIds)
    {
        await db.GameGenres.Where(x => x.GameId == gameId).ExecuteDeleteAsync();
        await db.GamePlatforms.Where(x => x.GameId == gameId).ExecuteDeleteAsync();
        db.GameGenres.AddRange(genreIds.Distinct().Select(id => new GameGenre { GameId = gameId, GenreId = id }));
        db.GamePlatforms.AddRange(platformIds.Distinct().Select(id => new GamePlatform { GameId = gameId, PlatformId = id }));
        await db.SaveChangesAsync();
    }

    private static async Task<IResult> DeactivateGameAsync(int id, GameCoreDbContext db)
    {
        var game = await db.Games.SingleOrDefaultAsync(x => x.GameId == id);
        if (game is null) return Results.NotFound();
        game.IsActive = false;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetCustomersAsync(GameCoreDbContext db) =>
        Results.Ok(await db.Customers.AsNoTracking().OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToListAsync());

    private static async Task<IResult> CreateCustomerAsync(CustomerWriteRequest request, GameCoreDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Results.BadRequest(new { error = "First and last name are required." });

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant()
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return Results.Created($"/api/customers/{customer.CustomerId}", customer);
    }

    private static async Task<IResult> UpdateCustomerAsync(int id, CustomerWriteRequest request, GameCoreDbContext db)
    {
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.CustomerId == id);
        if (customer is null) return Results.NotFound();
        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Phone = request.Phone?.Trim();
        customer.Email = request.Email?.Trim().ToLowerInvariant();
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetSalesAsync(GameCoreDbContext db)
    {
        var rows = await db.Sales.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Employee)
            .Include(x => x.SaleDetails).ThenInclude(x => x.Game)
            .OrderByDescending(x => x.SaleDate)
            .Select(x => new
            {
                x.SaleId, x.SaleDate, x.Status,
                Customer = x.Customer.FirstName + " " + x.Customer.LastName,
                Employee = x.Employee == null ? null : x.Employee.FirstName + " " + x.Employee.LastName,
                Total = x.SaleDetails.Sum(d => d.Quantity * d.UnitPrice)
            }).ToListAsync();
        return Results.Ok(rows);
    }

    private static async Task<IResult> CreateSaleAsync(SaleCreateRequest request, GameCoreDbContext db)
    {
        if (request.Items.Length == 0 || request.Items.Any(x => x.Quantity <= 0))
            return Results.BadRequest(new { error = "At least one valid sale item is required." });

        if (!await db.Customers.AnyAsync(x => x.CustomerId == request.CustomerId))
            return Results.BadRequest(new { error = "Customer does not exist." });

        if (request.EmployeeId is not null && !await db.Employees.AnyAsync(x => x.EmployeeId == request.EmployeeId && x.IsActive))
            return Results.BadRequest(new { error = "Employee does not exist or is inactive." });

        var ids = request.Items.Select(x => x.GameId).Distinct().ToArray();
        var games = await db.Games.Where(x => ids.Contains(x.GameId) && x.IsActive).ToDictionaryAsync(x => x.GameId);
        if (games.Count != ids.Length)
            return Results.BadRequest(new { error = "One or more games do not exist or are inactive." });

        await using var tx = await db.Database.BeginTransactionAsync();
        var sale = new Sale { CustomerId = request.CustomerId, EmployeeId = request.EmployeeId, Status = "Completed" };
        db.Sales.Add(sale);
        await db.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var game = games[item.GameId];
            db.SaleDetails.Add(new SaleDetail
            {
                SaleId = sale.SaleId, GameId = game.GameId, Quantity = item.Quantity, UnitPrice = game.UnitPrice
            });
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Results.Created($"/api/sales/{sale.SaleId}", new { sale.SaleId });
    }

    private static async Task<IResult> GetEmployeesAsync(GameCoreDbContext db) =>
        Results.Ok(await db.Employees.AsNoTracking()
            .Include(x => x.Branch).Include(x => x.JobPosition)
            .OrderBy(x => x.FirstName)
            .Select(x => new
            {
                x.EmployeeId, x.FirstName, x.LastName, x.Email, x.Phone, x.IsActive,
                Branch = x.Branch.Name, Position = x.JobPosition.Name
            }).ToListAsync());

    private static async Task<IResult> CreateEmployeeAsync(EmployeeWriteRequest request, GameCoreDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Results.BadRequest(new { error = "First and last name are required." });

        var employee = new Employee
        {
            BranchId = request.BranchId,
            JobPositionId = request.JobPositionId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim().ToLowerInvariant(),
            AddressLine = request.AddressLine?.Trim(),
            IsActive = request.IsActive
        };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return Results.Created($"/api/employees/{employee.EmployeeId}", new { employee.EmployeeId });
    }

    private static async Task<IResult> UpdateEmployeeAsync(int id, EmployeeWriteRequest request, GameCoreDbContext db)
    {
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.EmployeeId == id);
        if (employee is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return Results.BadRequest(new { error = "First and last name are required." });

        employee.BranchId = request.BranchId;
        employee.JobPositionId = request.JobPositionId;
        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Phone = request.Phone?.Trim();
        employee.Email = request.Email?.Trim().ToLowerInvariant();
        employee.AddressLine = request.AddressLine?.Trim();
        employee.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> GetDistributionsAsync(GameCoreDbContext db) =>
        Results.Ok(await db.Distributions.AsNoTracking()
            .Include(x => x.Game).Include(x => x.Country)
            .OrderByDescending(x => x.DistributionDate)
            .Select(x => new
            {
                x.DistributionId, x.GameId, Game = x.Game.Title, x.CountryId,
                Country = x.Country.Name, x.DistributionDate, x.Units
            }).ToListAsync());

    private static async Task<IResult> UpsertDistributionAsync(DistributionWriteRequest request, GameCoreDbContext db)
    {
        if (request.Units < 0) return Results.BadRequest(new { error = "Units cannot be negative." });
        var row = await db.Distributions.SingleOrDefaultAsync(x =>
            x.GameId == request.GameId && x.CountryId == request.CountryId && x.DistributionDate == request.DistributionDate);
        if (row is null)
        {
            row = new Distribution
            {
                GameId = request.GameId, CountryId = request.CountryId,
                DistributionDate = request.DistributionDate, Units = request.Units
            };
            db.Distributions.Add(row);
        }
        else row.Units = request.Units;
        await db.SaveChangesAsync();
        return Results.Ok(new { row.DistributionId });
    }

    private static async Task<IResult> GetCatalogsAsync(GameCoreDbContext db) =>
        Results.Ok(new
        {
            genres = await db.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            platforms = await db.Platforms.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            ratings = await db.AgeRatings.AsNoTracking().OrderBy(x => x.Code).ToListAsync(),
            countries = await db.Countries.AsNoTracking().OrderBy(x => x.Name).ToListAsync(),
            branches = await db.Branches.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(),
            positions = await db.JobPositions.AsNoTracking().OrderBy(x => x.Name).ToListAsync()
        });

    private static async Task<IResult> GetDashboardAsync(GameCoreDbContext db)
    {
        var completedIds = db.Sales
            .Where(x => x.Status == "Completed")
            .Select(x => x.SaleId);

        var revenue = await db.SaleDetails
            .Where(x => completedIds.Contains(x.SaleId))
            .SumAsync(x => (decimal?)(x.Quantity * x.UnitPrice)) ?? 0m;

        var topGames = await db.SaleDetails.AsNoTracking()
            .Where(x => x.Sale.Status == "Completed")
            .GroupBy(x => new { x.GameId, x.Game.Title })
            .Select(g => new
            {
                g.Key.GameId,
                g.Key.Title,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.UnitsSold)
            .ThenByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        var monthlySales = await db.Sales.AsNoTracking()
            .Where(x => x.Status == "Completed")
            .Select(x => new
            {
                x.SaleDate,
                Total = x.SaleDetails.Sum(d => d.Quantity * d.UnitPrice)
            })
            .GroupBy(x => new { x.SaleDate.Year, x.SaleDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                SalesCount = g.Count(),
                Revenue = g.Sum(x => x.Total)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .Take(12)
            .ToListAsync();

        var recentSales = await db.Sales.AsNoTracking()
            .Where(x => x.Status == "Completed")
            .OrderByDescending(x => x.SaleDate)
            .Take(5)
            .Select(x => new
            {
                x.SaleId,
                x.SaleDate,
                Customer = x.Customer.FirstName + " " + x.Customer.LastName,
                Total = x.SaleDetails.Sum(d => d.Quantity * d.UnitPrice)
            })
            .ToListAsync();

        var distributionByCountry = await db.Distributions.AsNoTracking()
            .GroupBy(x => new { x.CountryId, x.Country.Name })
            .Select(g => new
            {
                g.Key.CountryId,
                Country = g.Key.Name,
                Units = g.Sum(x => x.Units)
            })
            .OrderByDescending(x => x.Units)
            .Take(5)
            .ToListAsync();

        return Results.Ok(new
        {
            games = await db.Games.CountAsync(x => x.IsActive),
            customers = await db.Customers.CountAsync(),
            sales = await db.Sales.CountAsync(x => x.Status == "Completed"),
            employees = await db.Employees.CountAsync(x => x.IsActive),
            revenue,
            topGames,
            monthlySales,
            recentSales,
            distributionByCountry
        });
    }

    private static async Task<IResult> GetReportsAsync(GameCoreDbContext db)
    {
        var monthlySales = await db.Sales.AsNoTracking()
            .Where(x => x.Status == "Completed")
            .Select(x => new
            {
                x.SaleDate,
                Total = x.SaleDetails.Sum(d => d.Quantity * d.UnitPrice)
            })
            .GroupBy(x => new { x.SaleDate.Year, x.SaleDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Sales = g.Count(),
                Revenue = g.Sum(x => x.Total)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var topGames = await db.SaleDetails.AsNoTracking()
            .Where(x => x.Sale.Status == "Completed")
            .GroupBy(x => new { x.GameId, x.Game.Title })
            .Select(g => new
            {
                g.Key.GameId,
                g.Key.Title,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.UnitsSold)
            .ToListAsync();

        var customers = await db.Customers.AsNoTracking()
            .Select(x => new
            {
                x.CustomerId,
                Customer = x.FirstName + " " + x.LastName,
                Sales = x.Sales.Count(s => s.Status == "Completed"),
                LifetimeValue = x.Sales
                    .Where(s => s.Status == "Completed")
                    .SelectMany(s => s.SaleDetails)
                    .Sum(d => (decimal?)(d.Quantity * d.UnitPrice)) ?? 0m
            })
            .OrderByDescending(x => x.LifetimeValue)
            .ToListAsync();

        var countries = await db.Distributions.AsNoTracking()
            .GroupBy(x => new { x.CountryId, x.Country.Name })
            .Select(g => new
            {
                g.Key.CountryId,
                Country = g.Key.Name,
                DistributedUnits = g.Sum(x => x.Units),
                Games = g.Select(x => x.GameId).Distinct().Count()
            })
            .OrderByDescending(x => x.DistributedUnits)
            .ToListAsync();

        return Results.Ok(new { monthlySales, topGames, customers, countries });
    }

    private static bool VerifyPassword(string password, string encoded)
    {
        var parts = encoded.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, string Email, string Role);
public sealed record GameWriteRequest(string Title, string? Story, DateOnly? ReleaseDate, decimal UnitPrice, int? AgeRatingId, int[] GenreIds, int[] PlatformIds, bool IsActive = true);
public sealed record CustomerWriteRequest(string FirstName, string LastName, string? Phone, string? Email);
public sealed record SaleItemRequest(int GameId, int Quantity);
public sealed record SaleCreateRequest(int CustomerId, int? EmployeeId, SaleItemRequest[] Items);
public sealed record EmployeeWriteRequest(int BranchId, int JobPositionId, string FirstName, string LastName, string? Phone, string? Email, string? AddressLine, bool IsActive = true);
public sealed record DistributionWriteRequest(int GameId, int CountryId, DateOnly DistributionDate, int Units);
