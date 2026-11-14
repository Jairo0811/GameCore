using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GameCore.Domain.Entities;
using GameCore.Infrastructure.Persistence;
using GameCore.WebAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameCore.Tests.SqlServer;

/// <summary>
/// Runs only when GAMECORE_TEST_SQLSERVER contains a connection string to a GameCoreDB
/// created with database/setup.sql (CI does this with a disposable SQL Server container).
/// These tests WRITE demo rows (a sale, a distribution); do not point them at data you care about.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string Variable = "GAMECORE_TEST_SQLSERVER";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
            Skip = $"Set {Variable} to a GameCoreDB connection string to run SQL Server integration tests.";
    }

    public static string ConnectionString => Environment.GetEnvironmentVariable(Variable)!;
}

/// <summary>The real API configuration (SQL Server provider + retry strategy) with an overridden connection string.</summary>
public sealed class SqlServerApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:GameCoreDb", connectionString);
    }
}

public sealed class SqlServerIntegrationTests
{
    [SqlServerFact]
    public async Task Customers_AllowSeveralNullEmails_ButRejectDuplicateValues()
    {
        await using var db = CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();

        db.Customers.AddRange(new Customer { FirstName = "Null", LastName = "A" }, new Customer { FirstName = "Null", LastName = "B" });
        await db.SaveChangesAsync();

        db.Customers.AddRange(
            new Customer { FirstName = "Dup", LastName = "A", Email = "integration.dup@gamecore.local" },
            new Customer { FirstName = "Dup", LastName = "B", Email = "integration.dup@gamecore.local" });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains(((SqlException)error.InnerException!).Number, new[] { 2601, 2627 });

        await transaction.RollbackAsync();
    }

    [SqlServerFact]
    public async Task Employees_AllowSeveralNullEmails_ButRejectDuplicateValues()
    {
        await using var db = CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var branchId = await db.Branches.Select(x => x.BranchId).FirstAsync();
        var positionId = await db.JobPositions.Select(x => x.JobPositionId).FirstAsync();

        db.Employees.AddRange(
            new Employee { BranchId = branchId, JobPositionId = positionId, FirstName = "Null", LastName = "A" },
            new Employee { BranchId = branchId, JobPositionId = positionId, FirstName = "Null", LastName = "B" });
        await db.SaveChangesAsync();

        db.Employees.AddRange(
            new Employee { BranchId = branchId, JobPositionId = positionId, FirstName = "Dup", LastName = "A", Email = "integration.dup@gamecore.local" },
            new Employee { BranchId = branchId, JobPositionId = positionId, FirstName = "Dup", LastName = "B", Email = "integration.dup@gamecore.local" });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains(((SqlException)error.InnerException!).Number, new[] { 2601, 2627 });

        await transaction.RollbackAsync();
    }

    [SqlServerFact]
    public async Task Inactive_IsPersisted_NotReplacedByTheDatabaseDefault()
    {
        await using var db = CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var employee = new Employee
        {
            BranchId = await db.Branches.Select(x => x.BranchId).FirstAsync(),
            JobPositionId = await db.JobPositions.Select(x => x.JobPositionId).FirstAsync(),
            FirstName = "Inactive", LastName = "Integration", IsActive = false
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var stored = await db.Employees.AsNoTracking().Where(x => x.EmployeeId == employee.EmployeeId).Select(x => x.IsActive).SingleAsync();
        Assert.False(stored);
        await transaction.RollbackAsync();
    }

    [SqlServerFact]
    public async Task Api_EndToEnd_AgainstSqlServer()
    {
        await using var factory = new SqlServerApiFactory(SqlServerFactAttribute.ConnectionString);
        var anonymous = factory.CreateClient();

        var health = await anonymous.GetFromJsonAsync<JsonElement>("/health");
        Assert.Equal("Healthy", health.GetProperty("status").GetString());

        var admin = await LoginAsync(factory, "admin@gamecore.local");
        var viewer = await LoginAsync(factory, "viewer@gamecore.local");
        var manager = await LoginAsync(factory, "manager@gamecore.local");

        // Read endpoints, including the decimal aggregations used by Dashboard and Reports.
        foreach (var path in new[] { "/api/dashboard", "/api/reports", "/api/games", "/api/customers", "/api/sales", "/api/employees", "/api/distributions", "/api/catalogs" })
            Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync(path)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync("/api/customers", new { firstName = "No", lastName = "Permitido" })).StatusCode);

        var games = await admin.GetFromJsonAsync<JsonElement>("/api/games");
        var gameIds = games.EnumerateArray().Where(x => x.GetProperty("isActive").GetBoolean()).Select(x => x.GetProperty("gameId").GetInt32()).Take(2).ToArray();
        var customerId = (await admin.GetFromJsonAsync<JsonElement>("/api/customers"))[0].GetProperty("customerId").GetInt32();

        // Sale with a user transaction under EnableRetryOnFailure (execution strategy).
        var sale = await manager.PostAsJsonAsync("/api/sales", new { customerId, employeeId = (int?)null, items = gameIds.Select(id => new { gameId = id, quantity = 1 }) });
        Assert.Equal(HttpStatusCode.Created, sale.StatusCode);

        var duplicated = await manager.PostAsJsonAsync("/api/sales", new { customerId, employeeId = (int?)null, items = new[] { new { gameId = gameIds[0], quantity = 1 }, new { gameId = gameIds[0], quantity = 1 } } });
        Assert.Equal(HttpStatusCode.BadRequest, duplicated.StatusCode);

        var invalidGenre = await manager.PostAsJsonAsync("/api/games", new { title = $"Integration {Guid.NewGuid():N}", unitPrice = 1m, genreIds = new[] { int.MaxValue }, platformIds = Array.Empty<int>() });
        Assert.Equal(HttpStatusCode.BadRequest, invalidGenre.StatusCode);

        // Distribution create + edit in place.
        var countryId = (await admin.GetFromJsonAsync<JsonElement>("/api/catalogs")).GetProperty("countries")[0].GetProperty("countryId").GetInt32();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-Random.Shared.Next(1, 3000));
        var created = await manager.PostAsJsonAsync("/api/distributions", new { gameId = gameIds[0], countryId, distributionDate = date, units = 5 });
        Assert.True(created.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK);
        var distributionId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("distributionId").GetInt64();
        var updated = await manager.PutAsJsonAsync($"/api/distributions/{distributionId}", new { gameId = gameIds[0], countryId, distributionDate = date, units = 9 });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    }

    private static GameCoreDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GameCoreDbContext>().UseSqlServer(SqlServerFactAttribute.ConnectionString).Options);

    private static async Task<HttpClient> LoginAsync(SqlServerApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "GameCore123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Token);
        return client;
    }
}
