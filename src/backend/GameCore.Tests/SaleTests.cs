using System.Net;
using System.Net.Http.Json;
using GameCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameCore.Tests;

public sealed class SaleTests : ApiTestBase
{
    [Fact]
    public async Task CreateSale_WithValidItems_StoresSaleAndHistoricalPrices()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/sales", new
        {
            customerId = ids.CustomerWithEmailId,
            employeeId = ids.ActiveEmployeeId,
            items = new[] { new { gameId = ids.ActiveGameId, quantity = 2 }, new { gameId = ids.SecondGameId, quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saleId = (await ReadJsonAsync(response)).GetProperty("saleId").GetInt64();
        var details = await Factory.QueryAsync(db => db.SaleDetails.Where(x => x.SaleId == saleId).OrderBy(x => x.GameId).ToListAsync());
        Assert.Equal(2, details.Count);
        Assert.Contains(details, x => x.GameId == ids.ActiveGameId && x.Quantity == 2 && x.UnitPrice == 39.99m);
        Assert.Contains(details, x => x.GameId == ids.SecondGameId && x.Quantity == 1 && x.UnitPrice == 59.99m);
        Assert.Equal("Completed", await Factory.QueryAsync(db => db.Sales.Where(x => x.SaleId == saleId).Select(x => x.Status).SingleAsync()));
    }

    [Fact]
    public async Task CreateSale_KeepsHistoricalPriceAfterGamePriceChanges()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();
        var response = await client.PostAsJsonAsync("/api/sales", new { customerId = ids.CustomerWithEmailId, employeeId = (int?)null, items = new[] { new { gameId = ids.ActiveGameId, quantity = 1 } } });
        var saleId = (await ReadJsonAsync(response)).GetProperty("saleId").GetInt64();

        var update = await client.PutAsJsonAsync($"/api/games/{ids.ActiveGameId}", new
        {
            title = "Pokémon Sun", story = (string?)null, releaseDate = "2016-11-18", unitPrice = 19.99m,
            ageRatingId = (int?)null, genreIds = Array.Empty<int>(), platformIds = Array.Empty<int>(), isActive = true
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        Assert.Equal(39.99m, await Factory.QueryAsync(db => db.SaleDetails.Where(x => x.SaleId == saleId).Select(x => x.UnitPrice).SingleAsync()));
    }

    [Fact]
    public async Task CreateSale_WithDuplicatedGameId_Returns400BeforeTouchingTheDatabase()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/sales", new
        {
            customerId = ids.CustomerWithEmailId,
            employeeId = (int?)null,
            items = new[] { new { gameId = ids.ActiveGameId, quantity = 1 }, new { gameId = ids.ActiveGameId, quantity = 3 } }
        });

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains("no puede repetirse", Errors(problem));
        Assert.Equal(0, await Factory.QueryAsync(db => db.Sales.CountAsync()));
    }

    public static TheoryData<string> InvalidSales => new()
    {
        "empty-items", "null-items", "zero-quantity", "negative-quantity", "unknown-customer",
        "inactive-employee", "unknown-employee", "inactive-game", "unknown-game"
    };

    [Theory]
    [MemberData(nameof(InvalidSales))]
    public async Task CreateSale_WithInvalidPayload_Returns400AndStoresNothing(string scenario)
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();
        var item = new { gameId = ids.ActiveGameId, quantity = 1 };
        object payload = scenario switch
        {
            "empty-items" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)null, items = Array.Empty<object>() },
            "null-items" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)null, items = (object[]?)null },
            "zero-quantity" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)null, items = new[] { new { gameId = ids.ActiveGameId, quantity = 0 } } },
            "negative-quantity" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)null, items = new[] { new { gameId = ids.ActiveGameId, quantity = -2 } } },
            "unknown-customer" => new { customerId = 9999, employeeId = (int?)null, items = new[] { item } },
            "inactive-employee" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)ids.InactiveEmployeeId, items = new[] { item } },
            "unknown-employee" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)9999, items = new[] { item } },
            "inactive-game" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)null, items = new[] { new { gameId = ids.InactiveGameId, quantity = 1 } } },
            "unknown-game" => new { customerId = ids.CustomerWithEmailId, employeeId = (int?)null, items = new[] { new { gameId = 9999, quantity = 1 } } },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var response = await client.PostAsJsonAsync("/api/sales", payload);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(0, await Factory.QueryAsync(db => db.Sales.CountAsync()));
        Assert.Equal(0, await Factory.QueryAsync(db => db.SaleDetails.CountAsync()));
    }
}
