using System.Net.Http.Headers;
using System.Net.Http.Json;
using GameCore.Domain.Entities;
using GameCore.Infrastructure.Persistence;
using GameCore.WebAPI;
using GameCore.WebAPI.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameCore.Tests.Infrastructure;

/// <summary>
/// Runs the real API pipeline (authentication, authorization, validation, ProblemDetails)
/// against an isolated in-memory SQLite database created from the EF Core model.
/// Each factory instance owns its own database, so tests never share state.
/// SQL Server specific behaviour is covered by <see cref="SqlServer.SqlServerIntegrationTests"/>.
/// </summary>
public sealed class GameCoreApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "GameCore123!";
    public const string AdminEmail = "admin@gamecore.local";
    public const string ManagerEmail = "manager@gamecore.local";
    public const string ViewerEmail = "viewer@gamecore.local";

    private readonly SqliteConnection _connection;
    private readonly string? _connectionString;

    public GameCoreApiFactory() : this(null) { }

    /// <param name="sqliteConnectionString">Overrides the in-memory database (used to simulate an unreachable database).</param>
    public GameCoreApiFactory(string? sqliteConnectionString)
    {
        _connectionString = sqliteConnectionString;
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GameCoreDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GameCoreDbContext>>();
            services.AddDbContext<GameCoreDbContext>(options =>
            {
                // Production uses EnableRetryOnFailure; a retrying strategy here makes the tests
                // fail if a user transaction is opened outside CreateExecutionStrategy().
                if (_connectionString is null) options.UseSqlite(_connection, sqlite => sqlite.ExecutionStrategy(d => new RetryingTestExecutionStrategy(d)));
                else options.UseSqlite(_connectionString, sqlite => sqlite.ExecutionStrategy(d => new RetryingTestExecutionStrategy(d)));
                options.ReplaceService<IModelCustomizer, SqliteModelCustomizer>();
            });
        });
    }

    /// <summary>Creates the schema from the EF model and loads a small, deterministic data set.</summary>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameCoreDbContext>();
        await db.Database.EnsureCreatedAsync();

        var company = new Company { Name = "Test Studios" };
        var activeBranch = new Branch { Company = company, Name = "Central", IsActive = true };
        var programmer = new JobPosition { Name = "Programador" };
        var designer = new JobPosition { Name = "Diseñador" };
        db.AddRange(company, activeBranch, programmer, designer);

        db.AppUsers.AddRange(
            new AppUser { Email = AdminEmail, PasswordHash = PasswordHasher.Hash(Password, 1_000), Role = Roles.Admin, IsActive = true },
            new AppUser { Email = ManagerEmail, PasswordHash = PasswordHasher.Hash(Password, 1_000), Role = Roles.Manager, IsActive = true },
            new AppUser { Email = ViewerEmail, PasswordHash = PasswordHasher.Hash(Password, 1_000), Role = Roles.Viewer, IsActive = true },
            new AppUser { Email = "disabled@gamecore.local", PasswordHash = PasswordHasher.Hash(Password, 1_000), Role = Roles.Admin, IsActive = false });

        db.Employees.AddRange(
            new Employee { Branch = activeBranch, JobPosition = programmer, FirstName = "Valeria", LastName = "Peña", Email = "valeria@test.local", IsActive = true },
            new Employee { Branch = activeBranch, JobPosition = designer, FirstName = "Inactivo", LastName = "Demo", IsActive = false });

        db.Customers.AddRange(
            new Customer { FirstName = "Ana", LastName = "Torres", Email = "ana@test.local" },
            new Customer { FirstName = "Carlos", LastName = "Méndez" });

        var rating = new AgeRating { Code = "E", Name = "Para Todos" };
        var adventure = new Genre { Name = "Aventura" };
        var racing = new Genre { Name = "Carreras" };
        var pc = new Platform { Name = "PC" };
        var console = new Platform { Name = "Consola" };
        var dominicanRepublic = new Country { Iso2 = "DO", Name = "República Dominicana" };
        var mexico = new Country { Iso2 = "MX", Name = "México" };
        db.AddRange(rating, adventure, racing, pc, console, dominicanRepublic, mexico);

        var activeGame = new Game { Title = "Pokémon Sun", ReleaseDate = new DateOnly(2016, 11, 18), UnitPrice = 39.99m, AgeRating = rating, IsActive = true };
        activeGame.GameGenres.Add(new GameGenre { Genre = adventure });
        activeGame.GamePlatforms.Add(new GamePlatform { Platform = console });
        var secondGame = new Game { Title = "Need for Speed", ReleaseDate = new DateOnly(2015, 11, 3), UnitPrice = 59.99m, IsActive = true };
        var inactiveGame = new Game { Title = "Retired Game", ReleaseDate = new DateOnly(2010, 1, 1), UnitPrice = 9.99m, IsActive = false };
        db.Games.AddRange(activeGame, secondGame, inactiveGame);

        db.Distributions.AddRange(
            new Distribution { Game = activeGame, Country = dominicanRepublic, DistributionDate = new DateOnly(2016, 11, 18), Units = 120 },
            new Distribution { Game = secondGame, Country = mexico, DistributionDate = new DateOnly(2015, 11, 3), Units = 250 });

        await db.SaveChangesAsync();
    }

    public async Task<HttpClient> CreateClientAsAsync(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Token);
        return client;
    }

    public async Task<T> QueryAsync<T>(Func<GameCoreDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameCoreDbContext>();
        return await query(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

/// <summary>
/// Adapts the two SQL Server specific details of the production model so it can be created
/// unchanged in SQLite: SYSUTCDATETIME() defaults and nvarchar(max) column types.
/// </summary>
public sealed class SqliteModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
        {
            if (string.Equals(property.GetDefaultValueSql(), "SYSUTCDATETIME()", StringComparison.OrdinalIgnoreCase))
                property.SetDefaultValueSql("CURRENT_TIMESTAMP");
            if (property.GetColumnType()?.Contains("(max)", StringComparison.OrdinalIgnoreCase) == true)
                property.SetColumnType(null);
        }
    }
}

/// <summary>
/// Behaves like SqlServerRetryingExecutionStrategy for transaction rules (RetriesOnFailure = true)
/// without actually retrying anything.
/// </summary>
public sealed class RetryingTestExecutionStrategy(ExecutionStrategyDependencies dependencies)
    : ExecutionStrategy(dependencies, maxRetryCount: 3, maxRetryDelay: TimeSpan.FromMilliseconds(10))
{
    protected override bool ShouldRetryOn(Exception exception) => false;
}
