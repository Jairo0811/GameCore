using System.Net;
using System.Net.Http.Json;
using GameCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameCore.Tests;

public sealed class DistributionTests : ApiTestBase
{
    [Fact]
    public async Task Put_ChangingTheNaturalKey_UpdatesTheSameRowInsteadOfCreatingAnother()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();
        var countBefore = await Factory.QueryAsync(db => db.Distributions.CountAsync());

        var response = await client.PutAsJsonAsync($"/api/distributions/{ids.FirstDistributionId}", new
        {
            gameId = ids.ActiveGameId, countryId = ids.MexicoId, distributionDate = "2017-01-15", units = 75
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(countBefore, await Factory.QueryAsync(db => db.Distributions.CountAsync()));
        var row = await Factory.QueryAsync(db => db.Distributions.SingleAsync(x => x.DistributionId == ids.FirstDistributionId));
        Assert.Equal(ids.MexicoId, row.CountryId);
        Assert.Equal(new DateOnly(2017, 1, 15), row.DistributionDate);
        Assert.Equal(75, row.Units);
    }

    [Fact]
    public async Task Put_OntoAnotherRowsKey_Returns409()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        // Second seeded row: Need for Speed / México / 2015-11-03.
        var response = await client.PutAsJsonAsync($"/api/distributions/{ids.FirstDistributionId}", new
        {
            gameId = ids.SecondGameId, countryId = ids.MexicoId, distributionDate = "2015-11-03", units = 10
        });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("unknown-id")]
    [InlineData("negative-units")]
    [InlineData("unknown-game")]
    [InlineData("unknown-country")]
    [InlineData("missing-date")]
    public async Task Put_Invalid(string scenario)
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();
        var id = scenario == "unknown-id" ? 9999 : ids.FirstDistributionId;
        object payload = scenario switch
        {
            "negative-units" => new { gameId = ids.ActiveGameId, countryId = ids.MexicoId, distributionDate = "2017-01-15", units = -1 },
            "unknown-game" => new { gameId = 9999, countryId = ids.MexicoId, distributionDate = "2017-01-15", units = 1 },
            "unknown-country" => new { gameId = ids.ActiveGameId, countryId = 9999, distributionDate = "2017-01-15", units = 1 },
            "missing-date" => new { gameId = ids.ActiveGameId, countryId = ids.MexicoId, units = 1 },
            _ => new { gameId = ids.ActiveGameId, countryId = ids.MexicoId, distributionDate = "2017-01-15", units = 1 }
        };

        var response = await client.PutAsJsonAsync($"/api/distributions/{id}", payload);

        await AssertProblemAsync(response, scenario == "unknown-id" ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NewKey_Creates_AndExistingKey_UpdatesUnits()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();
        var payload = new { gameId = ids.SecondGameId, countryId = ids.DominicanRepublicId, distributionDate = "2020-02-02", units = 10 };

        var created = await client.PostAsJsonAsync("/api/distributions", payload);
        var updated = await client.PostAsJsonAsync("/api/distributions", payload with { units = 40 });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var rows = await Factory.QueryAsync(db => db.Distributions.Where(x => x.GameId == ids.SecondGameId && x.CountryId == ids.DominicanRepublicId).ToListAsync());
        Assert.Equal(40, Assert.Single(rows).Units);
    }
}
