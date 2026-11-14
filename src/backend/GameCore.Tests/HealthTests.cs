using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameCore.Infrastructure.Persistence;
using GameCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameCore.Tests;

public sealed class HealthTests
{
    [Fact]
    public async Task Health_WithReachableDatabase_IsHealthy()
    {
        await using var factory = new GameCoreApiFactory();
        await factory.SeedAsync();

        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        Assert.Equal("database", body.GetProperty("checks")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Health_WithUnreachableDatabase_Returns503Unhealthy()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.db");
        await using var factory = new GameCoreApiFactory($"Data Source={missing};Mode=ReadOnly");

        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Unhealthy", body.GetProperty("status").GetString());
    }
}

public sealed class ConfigurationTests
{
    [Fact]
    public void ProductionConnectionString_IsReadFromConfiguration()
    {
        // Guards the SQL Server integration fixture, which overrides the connection string the same way.
        const string connectionString = "Server=example.invalid;Database=GameCoreDB;User Id=sa;Password=x;TrustServerCertificate=True";
        using var factory = new SqlServer.SqlServerApiFactory(connectionString);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameCoreDbContext>();

        Assert.True(db.Database.IsSqlServer());
        var actual = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.Database.GetConnectionString());
        Assert.Equal("example.invalid", actual.DataSource);
        Assert.Equal("GameCoreDB", actual.InitialCatalog);
    }

    [Fact]
    public async Task Cors_AllowsConfiguredOriginOnly()
    {
        await using var factory = new GameCoreApiFactory();
        var client = factory.CreateClient();

        var allowed = await client.SendAsync(Preflight("http://localhost:5173"));
        var denied = await client.SendAsync(Preflight("http://evil.example"));

        Assert.Equal("http://localhost:5173", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/games");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return request;
    }
}
