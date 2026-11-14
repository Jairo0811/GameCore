using System.Net;
using System.Net.Http.Json;
using GameCore.Infrastructure.Persistence;
using GameCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameCore.Tests;

public sealed class AuthorizationTests : ApiTestBase
{
    public static TheoryData<string> ReadEndpoints => new()
    {
        "/api/games", "/api/customers", "/api/sales", "/api/employees",
        "/api/distributions", "/api/catalogs"
    };

    public static TheoryData<string, string> WriteEndpoints => new()
    {
        { "POST", "/api/games" },
        { "PUT", "/api/games/1" },
        { "DELETE", "/api/games/1" },
        { "POST", "/api/customers" },
        { "PUT", "/api/customers/1" },
        { "POST", "/api/sales" },
        { "POST", "/api/employees" },
        { "PUT", "/api/employees/1" },
        { "POST", "/api/distributions" },
        { "PUT", "/api/distributions/1" }
    };

    [Theory]
    [MemberData(nameof(ReadEndpoints))]
    public async Task Viewer_CanRead(string path)
    {
        var client = await ViewerAsync();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(WriteEndpoints))]
    public async Task Viewer_CannotWrite_AndNothingChanges(string method, string path)
    {
        var client = await ViewerAsync();
        var before = await Factory.QueryAsync(CountRowsAsync);

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "DELETE" ? null : JsonContent.Create(new { })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await Factory.QueryAsync(CountRowsAsync));
        Assert.Equal(2, await Factory.QueryAsync(db => db.Games.CountAsync(x => x.IsActive)));
    }

    [Fact]
    public async Task Manager_CanCreateAndUpdate()
    {
        var client = await ManagerAsync();

        var created = await client.PostAsJsonAsync("/api/customers", new { firstName = "Lucía", lastName = "Gómez", phone = (string?)null, email = "lucia@test.local" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await ReadJsonAsync(created)).GetProperty("customerId").GetInt32();

        var updated = await client.PutAsJsonAsync($"/api/customers/{id}", new { firstName = "Lucía", lastName = "Gómez Ruiz", phone = "809-555-0000", email = "lucia@test.local" });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
    }

    [Fact]
    public async Task Admin_CanDeactivateGame()
    {
        var client = await AdminAsync();
        var gameId = await Factory.QueryAsync(db => db.Games.Where(x => x.Title == "Need for Speed").Select(x => x.GameId).SingleAsync());

        var response = await client.DeleteAsync($"/api/games/{gameId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False((await Factory.QueryAsync(db => db.Games.FindAsync(gameId).AsTask()))!.IsActive);
    }

    private static async Task<string> CountRowsAsync(GameCoreDbContext db) =>
        string.Join(',',
            await db.Games.CountAsync(),
            await db.Customers.CountAsync(),
            await db.Sales.CountAsync(),
            await db.Employees.CountAsync(),
            await db.Distributions.CountAsync());
}
