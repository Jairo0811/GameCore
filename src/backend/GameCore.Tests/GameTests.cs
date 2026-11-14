using System.Net;
using System.Net.Http.Json;
using GameCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameCore.Tests;

public sealed class GameTests : ApiTestBase
{
    private static object Game(string title, int[] genreIds, int[] platformIds, bool isActive = true, decimal unitPrice = 49.99m, string? releaseDate = "2024-05-01") => new
    {
        title, story = "  Historia  ", releaseDate, unitPrice, ageRatingId = (int?)null, genreIds, platformIds, isActive
    };

    [Fact]
    public async Task CreateGame_WithRelations_StoresEverything()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/games", Game("  Nuevo Juego  ", [ids.AdventureGenreId, ids.RacingGenreId], [ids.PcPlatformId]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var gameId = (await ReadJsonAsync(response)).GetProperty("gameId").GetInt32();
        var game = await Factory.QueryAsync(db => db.Games.Include(x => x.GameGenres).Include(x => x.GamePlatforms).SingleAsync(x => x.GameId == gameId));
        Assert.Equal("Nuevo Juego", game.Title);
        Assert.Equal("Historia", game.Story);
        Assert.True(game.IsActive);
        Assert.Equal([ids.AdventureGenreId, ids.RacingGenreId], game.GameGenres.Select(x => x.GenreId).Order().ToArray());
        Assert.Equal([ids.PcPlatformId], game.GamePlatforms.Select(x => x.PlatformId).ToArray());
    }

    [Fact]
    public async Task CreateGame_RespectsIsActiveFalse()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/games", Game("Borrador", [ids.AdventureGenreId], [], isActive: false));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var gameId = (await ReadJsonAsync(response)).GetProperty("gameId").GetInt32();
        Assert.False(await Factory.QueryAsync(db => db.Games.Where(x => x.GameId == gameId).Select(x => x.IsActive).SingleAsync()));
    }

    [Theory]
    [InlineData("genre")]
    [InlineData("platform")]
    public async Task CreateGame_WithUnknownRelation_Returns400AndStoresNoGame(string relation)
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();
        var gamesBefore = await Factory.QueryAsync(db => db.Games.CountAsync());

        var payload = relation == "genre"
            ? Game("Juego Inválido", [ids.AdventureGenreId, 9999], [ids.PcPlatformId])
            : Game("Juego Inválido", [ids.AdventureGenreId], [ids.PcPlatformId, 9999]);
        var response = await client.PostAsJsonAsync("/api/games", payload);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(gamesBefore, await Factory.QueryAsync(db => db.Games.CountAsync()));
        Assert.False(await Factory.QueryAsync(db => db.Games.AnyAsync(x => x.Title == "Juego Inválido")));
    }

    [Fact]
    public async Task UpdateGame_WithUnknownRelation_KeepsOriginalGameAndRelations()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync($"/api/games/{ids.ActiveGameId}", Game("Título Cambiado", [9999], [ids.PcPlatformId]));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        var game = await Factory.QueryAsync(db => db.Games.Include(x => x.GameGenres).Include(x => x.GamePlatforms).SingleAsync(x => x.GameId == ids.ActiveGameId));
        Assert.Equal("Pokémon Sun", game.Title);
        Assert.Equal([ids.AdventureGenreId], game.GameGenres.Select(x => x.GenreId).ToArray());
        Assert.Equal([ids.ConsolePlatformId], game.GamePlatforms.Select(x => x.PlatformId).ToArray());
    }

    [Fact]
    public async Task UpdateGame_ReplacesRelations()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync($"/api/games/{ids.ActiveGameId}",
            Game("Pokémon Sun", [ids.AdventureGenreId, ids.RacingGenreId], [ids.PcPlatformId], releaseDate: "2016-11-18"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var game = await Factory.QueryAsync(db => db.Games.Include(x => x.GameGenres).Include(x => x.GamePlatforms).SingleAsync(x => x.GameId == ids.ActiveGameId));
        Assert.Equal([ids.AdventureGenreId, ids.RacingGenreId], game.GameGenres.Select(x => x.GenreId).Order().ToArray());
        Assert.Equal([ids.PcPlatformId], game.GamePlatforms.Select(x => x.PlatformId).ToArray());
    }

    [Theory]
    [InlineData("duplicated-genres")]
    [InlineData("blank-title")]
    [InlineData("negative-price")]
    [InlineData("three-decimals")]
    [InlineData("null-arrays-are-allowed")]
    public async Task CreateGame_Validation(string scenario)
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();
        object payload = scenario switch
        {
            "duplicated-genres" => Game("Juego", [ids.AdventureGenreId, ids.AdventureGenreId], []),
            "blank-title" => Game("   ", [], []),
            "negative-price" => Game("Juego", [], [], unitPrice: -1m),
            "three-decimals" => Game("Juego", [], [], unitPrice: 1.999m),
            "null-arrays-are-allowed" => new { title = "Juego sin relaciones", unitPrice = 10m, genreIds = (int[]?)null, platformIds = (int[]?)null },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var response = await client.PostAsJsonAsync("/api/games", payload);

        if (scenario == "null-arrays-are-allowed")
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGame_WithExistingTitleAndDate_Returns409()
    {
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/games", Game("Pokémon Sun", [], [], releaseDate: "2016-11-18"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateGame_Unknown_Returns404()
    {
        var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync("/api/games/9999", Game("Juego", [], []));

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
