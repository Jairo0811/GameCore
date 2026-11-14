using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameCore.Domain.Entities;
using GameCore.Infrastructure.Persistence;
using GameCore.WebAPI.Errors;
using GameCore.WebAPI.Security;
using GameCore.WebAPI.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GameCore.WebAPI;

public static class ApiEndpoints
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public static IEndpointRouteBuilder MapGameCoreApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");
        api.MapPost("/auth/login", LoginAsync).AllowAnonymous();

        // Every /api resource requires an authenticated GameCore role.
        // Viewer: GET only. Manager and Admin: GET + create/update/deactivate.
        var read = api.MapGroup(string.Empty).RequireAuthorization(Policies.Read);
        var write = api.MapGroup(string.Empty).RequireAuthorization(Policies.Read, Policies.Write);

        read.MapGet("/games", GetGamesAsync);
        write.MapPost("/games", CreateGameAsync);
        write.MapPut("/games/{id:int}", UpdateGameAsync);
        write.MapDelete("/games/{id:int}", DeactivateGameAsync);

        read.MapGet("/customers", GetCustomersAsync);
        write.MapPost("/customers", CreateCustomerAsync);
        write.MapPut("/customers/{id:int}", UpdateCustomerAsync);

        read.MapGet("/sales", GetSalesAsync);
        write.MapPost("/sales", CreateSaleAsync);

        read.MapGet("/employees", GetEmployeesAsync);
        write.MapPost("/employees", CreateEmployeeAsync);
        write.MapPut("/employees/{id:int}", UpdateEmployeeAsync);

        read.MapGet("/distributions", GetDistributionsAsync);
        write.MapPost("/distributions", UpsertDistributionAsync);
        write.MapPut("/distributions/{id:long}", UpdateDistributionAsync);

        read.MapGet("/catalogs", GetCatalogsAsync);
        read.MapGet("/dashboard", GetDashboardAsync);
        read.MapGet("/reports", GetReportsAsync);
        return endpoints;
    }

    // ---------------------------------------------------------------- auth

    private static async Task<IResult> LoginAsync(LoginRequest request, GameCoreDbContext db, IConfiguration configuration)
    {
        var errors = RequestValidator.ValidateLogin(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        var email = RequestValidator.NormalizeEmail(request.Email)!;
        var user = await db.AppUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Results.Unauthorized();

        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = configuration["Jwt:Issuer"] ?? "GameCore";
        var audience = configuration["Jwt:Audience"] ?? "GameCore.Client";
        var expiresAt = DateTime.UtcNow.Add(TokenLifetime);
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
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Email, user.Role, expiresAt));
    }

    // --------------------------------------------------------------- games

    private static async Task<IResult> GetGamesAsync(GameCoreDbContext db)
    {
        var rows = await db.Games.AsNoTracking()
            .Include(x => x.AgeRating)
            .Include(x => x.GameGenres).ThenInclude(x => x.Genre)
            .Include(x => x.GamePlatforms).ThenInclude(x => x.Platform)
            .OrderBy(x => x.Title)
            .Select(x => new
            {
                x.GameId, x.Title, x.Story, x.ReleaseDate, x.UnitPrice, x.IsActive, x.AgeRatingId,
                AgeRating = x.AgeRating == null ? null : x.AgeRating.Code,
                GenreIds = x.GameGenres.Select(g => g.GenreId).ToArray(),
                PlatformIds = x.GamePlatforms.Select(p => p.PlatformId).ToArray(),
                Genres = x.GameGenres.Select(g => g.Genre.Name).ToArray(),
                Platforms = x.GamePlatforms.Select(p => p.Platform.Name).ToArray()
            }).ToListAsync();
        return Results.Ok(rows);
    }

    /// <summary>
    /// Creates the game and its genre/platform relations in a single SaveChanges call,
    /// which EF Core executes inside one database transaction: either everything is
    /// stored or nothing is. <c>IsActive</c> is honoured (defaults to true when omitted).
    /// </summary>
    private static async Task<IResult> CreateGameAsync(GameWriteRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateGame(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        var title = RequestValidator.Normalize(request.Title)!;
        var problem = await CheckGameReferencesAsync(db, request, title, gameId: null);
        if (problem is not null) return problem;

        var game = new Game
        {
            Title = title,
            Story = RequestValidator.Normalize(request.Story),
            ReleaseDate = request.ReleaseDate,
            UnitPrice = request.UnitPrice,
            AgeRatingId = request.AgeRatingId,
            IsActive = request.IsActive
        };
        foreach (var genreId in request.GenreIds ?? []) game.GameGenres.Add(new GameGenre { GenreId = genreId });
        foreach (var platformId in request.PlatformIds ?? []) game.GamePlatforms.Add(new GamePlatform { PlatformId = platformId });

        db.Games.Add(game);
        await db.SaveChangesAsync();
        return Results.Created($"/api/games/{game.GameId}", new { game.GameId });
    }

    /// <summary>
    /// Updates the game and synchronises its relations (only the differences are
    /// deleted/inserted) in a single, atomic SaveChanges call.
    /// </summary>
    private static async Task<IResult> UpdateGameAsync(int id, GameWriteRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateGame(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        var game = await db.Games
            .Include(x => x.GameGenres)
            .Include(x => x.GamePlatforms)
            .SingleOrDefaultAsync(x => x.GameId == id);
        if (game is null) return ApiProblems.NotFound("El videojuego no existe.");

        var title = RequestValidator.Normalize(request.Title)!;
        var problem = await CheckGameReferencesAsync(db, request, title, id);
        if (problem is not null) return problem;

        game.Title = title;
        game.Story = RequestValidator.Normalize(request.Story);
        game.ReleaseDate = request.ReleaseDate;
        game.UnitPrice = request.UnitPrice;
        game.AgeRatingId = request.AgeRatingId;
        game.IsActive = request.IsActive;

        var genreIds = (request.GenreIds ?? []).ToHashSet();
        foreach (var relation in game.GameGenres.Where(x => !genreIds.Contains(x.GenreId)).ToList()) db.GameGenres.Remove(relation);
        foreach (var genreId in genreIds.Where(x => game.GameGenres.All(g => g.GenreId != x)).ToList()) game.GameGenres.Add(new GameGenre { GameId = id, GenreId = genreId });

        var platformIds = (request.PlatformIds ?? []).ToHashSet();
        foreach (var relation in game.GamePlatforms.Where(x => !platformIds.Contains(x.PlatformId)).ToList()) db.GamePlatforms.Remove(relation);
        foreach (var platformId in platformIds.Where(x => game.GamePlatforms.All(p => p.PlatformId != x)).ToList()) game.GamePlatforms.Add(new GamePlatform { GameId = id, PlatformId = platformId });

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult?> CheckGameReferencesAsync(GameCoreDbContext db, GameWriteRequest request, string title, int? gameId)
    {
        if (request.AgeRatingId is { } ratingId && !await db.AgeRatings.AnyAsync(x => x.AgeRatingId == ratingId))
            return ApiProblems.Validation("ageRatingId", "La clasificación indicada no existe.");

        var genreIds = request.GenreIds ?? [];
        if (genreIds.Length > 0 && await db.Genres.CountAsync(x => genreIds.Contains(x.GenreId)) != genreIds.Length)
            return ApiProblems.Validation("genreIds", "Uno o más géneros no existen.");

        var platformIds = request.PlatformIds ?? [];
        if (platformIds.Length > 0 && await db.Platforms.CountAsync(x => platformIds.Contains(x.PlatformId)) != platformIds.Length)
            return ApiProblems.Validation("platformIds", "Una o más plataformas no existen.");

        var excludedId = gameId ?? 0;
        if (await db.Games.AnyAsync(x => x.Title == title && x.ReleaseDate == request.ReleaseDate && x.GameId != excludedId))
            return ApiProblems.Conflict("Ya existe un videojuego con ese título y fecha de lanzamiento.");

        return null;
    }

    private static async Task<IResult> DeactivateGameAsync(int id, GameCoreDbContext db)
    {
        var game = await db.Games.SingleOrDefaultAsync(x => x.GameId == id);
        if (game is null) return ApiProblems.NotFound("El videojuego no existe.");
        game.IsActive = false;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // ----------------------------------------------------------- customers

    private static async Task<IResult> GetCustomersAsync(GameCoreDbContext db) =>
        Results.Ok(await db.Customers.AsNoTracking().OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToListAsync());

    private static async Task<IResult> CreateCustomerAsync(CustomerWriteRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateCustomer(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        var email = RequestValidator.NormalizeEmail(request.Email);
        if (email is not null && await db.Customers.AnyAsync(x => x.Email == email))
            return ApiProblems.Conflict("Ya existe un cliente con ese correo.");

        var customer = new Customer
        {
            FirstName = RequestValidator.Normalize(request.FirstName)!,
            LastName = RequestValidator.Normalize(request.LastName)!,
            Phone = RequestValidator.Normalize(request.Phone),
            Email = email
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return Results.Created($"/api/customers/{customer.CustomerId}", customer);
    }

    private static async Task<IResult> UpdateCustomerAsync(int id, CustomerWriteRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateCustomer(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        var customer = await db.Customers.SingleOrDefaultAsync(x => x.CustomerId == id);
        if (customer is null) return ApiProblems.NotFound("El cliente no existe.");

        var email = RequestValidator.NormalizeEmail(request.Email);
        if (email is not null && await db.Customers.AnyAsync(x => x.Email == email && x.CustomerId != id))
            return ApiProblems.Conflict("Ya existe otro cliente con ese correo.");

        customer.FirstName = RequestValidator.Normalize(request.FirstName)!;
        customer.LastName = RequestValidator.Normalize(request.LastName)!;
        customer.Phone = RequestValidator.Normalize(request.Phone);
        customer.Email = email;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // --------------------------------------------------------------- sales

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

    /// <summary>
    /// Validates the whole payload before writing (customer, active employee, active games,
    /// no repeated GameId) and stores Sale + SaleDetails atomically. The historical price is
    /// the game's UnitPrice at the moment of the sale.
    /// </summary>
    private static async Task<IResult> CreateSaleAsync(SaleCreateRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateSale(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        if (!await db.Customers.AnyAsync(x => x.CustomerId == request.CustomerId))
            return ApiProblems.Validation("customerId", "El cliente no existe.");

        if (request.EmployeeId is { } employeeId && !await db.Employees.AnyAsync(x => x.EmployeeId == employeeId && x.IsActive))
            return ApiProblems.Validation("employeeId", "El empleado no existe o está inactivo.");

        var ids = request.Items.Select(x => x.GameId).ToArray();
        var prices = await db.Games.AsNoTracking()
            .Where(x => ids.Contains(x.GameId) && x.IsActive)
            .ToDictionaryAsync(x => x.GameId, x => x.UnitPrice);
        if (prices.Count != ids.Length)
            return ApiProblems.Validation("items", "Uno o más videojuegos no existen o están inactivos.");

        // EnableRetryOnFailure requires user transactions to run inside the execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();
        var saleId = await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();

            var sale = new Sale { CustomerId = request.CustomerId, EmployeeId = request.EmployeeId, Status = "Completed" };
            foreach (var item in request.Items)
                sale.SaleDetails.Add(new SaleDetail { GameId = item.GameId, Quantity = item.Quantity, UnitPrice = prices[item.GameId] });

            db.Sales.Add(sale);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return sale.SaleId;
        });

        return Results.Created($"/api/sales/{saleId}", new { SaleId = saleId });
    }

    // ----------------------------------------------------------- employees

    private static async Task<IResult> GetEmployeesAsync(GameCoreDbContext db) =>
        Results.Ok(await db.Employees.AsNoTracking()
            .Include(x => x.Branch).Include(x => x.JobPosition)
            .OrderBy(x => x.FirstName)
            .Select(x => new
            {
                x.EmployeeId, x.BranchId, x.JobPositionId, x.FirstName, x.LastName, x.Email, x.Phone, x.AddressLine, x.IsActive,
                Branch = x.Branch.Name, Position = x.JobPosition.Name
            }).ToListAsync());

    private static async Task<IResult> CreateEmployeeAsync(EmployeeWriteRequest request, GameCoreDbContext db)
    {
        var problem = await ValidateEmployeeAsync(request, db, employeeId: null);
        if (problem is not null) return problem;

        var employee = new Employee();
        ApplyEmployee(employee, request);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return Results.Created($"/api/employees/{employee.EmployeeId}", new { employee.EmployeeId });
    }

    private static async Task<IResult> UpdateEmployeeAsync(int id, EmployeeWriteRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateEmployee(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        var employee = await db.Employees.SingleOrDefaultAsync(x => x.EmployeeId == id);
        if (employee is null) return ApiProblems.NotFound("El empleado no existe.");

        var problem = await ValidateEmployeeAsync(request, db, id);
        if (problem is not null) return problem;

        ApplyEmployee(employee, request);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult?> ValidateEmployeeAsync(EmployeeWriteRequest request, GameCoreDbContext db, int? employeeId)
    {
        var errors = RequestValidator.ValidateEmployee(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        if (!await db.Branches.AnyAsync(x => x.BranchId == request.BranchId))
            return ApiProblems.Validation("branchId", "La sucursal no existe.");
        if (!await db.JobPositions.AnyAsync(x => x.JobPositionId == request.JobPositionId))
            return ApiProblems.Validation("jobPositionId", "El puesto no existe.");

        var email = RequestValidator.NormalizeEmail(request.Email);
        var excludedId = employeeId ?? 0;
        if (email is not null && await db.Employees.AnyAsync(x => x.Email == email && x.EmployeeId != excludedId))
            return ApiProblems.Conflict("Ya existe un empleado con ese correo.");

        return null;
    }

    private static void ApplyEmployee(Employee employee, EmployeeWriteRequest request)
    {
        employee.BranchId = request.BranchId;
        employee.JobPositionId = request.JobPositionId;
        employee.FirstName = RequestValidator.Normalize(request.FirstName)!;
        employee.LastName = RequestValidator.Normalize(request.LastName)!;
        employee.Phone = RequestValidator.Normalize(request.Phone);
        employee.Email = RequestValidator.NormalizeEmail(request.Email);
        employee.AddressLine = RequestValidator.Normalize(request.AddressLine);
        employee.IsActive = request.IsActive;
    }

    // ------------------------------------------------------- distributions

    private static async Task<IResult> GetDistributionsAsync(GameCoreDbContext db) =>
        Results.Ok(await db.Distributions.AsNoTracking()
            .Include(x => x.Game).Include(x => x.Country)
            .OrderByDescending(x => x.DistributionDate)
            .Select(x => new
            {
                x.DistributionId, x.GameId, Game = x.Game.Title, x.CountryId,
                Country = x.Country.Name, x.DistributionDate, x.Units
            }).ToListAsync());

    /// <summary>
    /// POST keeps the documented upsert semantics of dbo.usp_RecordDistribution:
    /// a new Game + Country + Date creates a row (201); an existing one updates its units (200).
    /// Editing an existing row by id must use PUT /api/distributions/{id}.
    /// </summary>
    private static async Task<IResult> UpsertDistributionAsync(DistributionWriteRequest request, GameCoreDbContext db)
    {
        var problem = await ValidateDistributionAsync(request, db);
        if (problem is not null) return problem;

        var row = await db.Distributions.SingleOrDefaultAsync(x =>
            x.GameId == request.GameId && x.CountryId == request.CountryId && x.DistributionDate == request.DistributionDate);
        if (row is not null)
        {
            row.Units = request.Units;
            await db.SaveChangesAsync();
            return Results.Ok(new { row.DistributionId, Created = false });
        }

        row = new Distribution
        {
            GameId = request.GameId, CountryId = request.CountryId,
            DistributionDate = request.DistributionDate, Units = request.Units
        };
        db.Distributions.Add(row);
        await db.SaveChangesAsync();
        return Results.Created($"/api/distributions/{row.DistributionId}", new { row.DistributionId, Created = true });
    }

    /// <summary>
    /// Updates an existing distribution in place (including its Game, Country or Date).
    /// Moving it onto a Game + Country + Date already used by another row returns 409.
    /// </summary>
    private static async Task<IResult> UpdateDistributionAsync(long id, DistributionWriteRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateDistribution(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);

        var row = await db.Distributions.SingleOrDefaultAsync(x => x.DistributionId == id);
        if (row is null) return ApiProblems.NotFound("La distribución no existe.");

        var problem = await ValidateDistributionAsync(request, db);
        if (problem is not null) return problem;

        if (await db.Distributions.AnyAsync(x => x.DistributionId != id && x.GameId == request.GameId
                && x.CountryId == request.CountryId && x.DistributionDate == request.DistributionDate))
            return ApiProblems.Conflict("Ya existe otra distribución para ese videojuego, país y fecha.");

        row.GameId = request.GameId;
        row.CountryId = request.CountryId;
        row.DistributionDate = request.DistributionDate;
        row.Units = request.Units;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult?> ValidateDistributionAsync(DistributionWriteRequest request, GameCoreDbContext db)
    {
        var errors = RequestValidator.ValidateDistribution(request);
        if (errors.Count > 0) return ApiProblems.Validation(errors);
        if (!await db.Games.AnyAsync(x => x.GameId == request.GameId))
            return ApiProblems.Validation("gameId", "El videojuego no existe.");
        if (!await db.Countries.AnyAsync(x => x.CountryId == request.CountryId))
            return ApiProblems.Validation("countryId", "El país no existe.");
        return null;
    }

    // ----------------------------------------------------- catalogs / KPIs

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
}

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string Token, string Email, string Role, DateTime ExpiresAt);
public sealed record GameWriteRequest(string Title, string? Story, DateOnly? ReleaseDate, decimal UnitPrice, int? AgeRatingId, int[]? GenreIds, int[]? PlatformIds, bool IsActive = true);
public sealed record CustomerWriteRequest(string FirstName, string LastName, string? Phone, string? Email);
public sealed record SaleItemRequest(int GameId, int Quantity);
public sealed record SaleCreateRequest(int CustomerId, int? EmployeeId, SaleItemRequest[] Items);
public sealed record EmployeeWriteRequest(int BranchId, int JobPositionId, string FirstName, string LastName, string? Phone, string? Email, string? AddressLine, bool IsActive = true);
public sealed record DistributionWriteRequest(int GameId, int CountryId, DateOnly DistributionDate, int Units);
