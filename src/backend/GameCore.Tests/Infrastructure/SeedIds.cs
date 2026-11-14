using GameCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameCore.Tests.Infrastructure;

/// <summary>Identifiers of the rows created by <see cref="GameCoreApiFactory.SeedAsync"/>.</summary>
public sealed record SeedIds(
    int ActiveGameId, int SecondGameId, int InactiveGameId,
    int CustomerWithEmailId, int CustomerWithoutEmailId,
    int ActiveEmployeeId, int InactiveEmployeeId,
    int BranchId, int JobPositionId,
    int AdventureGenreId, int RacingGenreId, int PcPlatformId, int ConsolePlatformId,
    int DominicanRepublicId, int MexicoId,
    long FirstDistributionId, long SecondDistributionId)
{
    public static async Task<SeedIds> LoadAsync(GameCoreApiFactory factory) => await factory.QueryAsync(async (GameCoreDbContext db) =>
    {
        async Task<int> Game(string title) => await db.Games.Where(x => x.Title == title).Select(x => x.GameId).SingleAsync();
        async Task<int> Customer(string name) => await db.Customers.Where(x => x.FirstName == name).Select(x => x.CustomerId).SingleAsync();
        async Task<int> Employee(string name) => await db.Employees.Where(x => x.FirstName == name).Select(x => x.EmployeeId).SingleAsync();
        async Task<int> Genre(string name) => await db.Genres.Where(x => x.Name == name).Select(x => x.GenreId).SingleAsync();
        async Task<int> Platform(string name) => await db.Platforms.Where(x => x.Name == name).Select(x => x.PlatformId).SingleAsync();
        async Task<int> Country(string iso) => await db.Countries.Where(x => x.Iso2 == iso).Select(x => x.CountryId).SingleAsync();
        var distributions = await db.Distributions.OrderBy(x => x.Units).Select(x => x.DistributionId).ToListAsync();

        return new SeedIds(
            await Game("Pokémon Sun"), await Game("Need for Speed"), await Game("Retired Game"),
            await Customer("Ana"), await Customer("Carlos"),
            await Employee("Valeria"), await Employee("Inactivo"),
            await db.Branches.Select(x => x.BranchId).SingleAsync(),
            await db.JobPositions.Where(x => x.Name == "Programador").Select(x => x.JobPositionId).SingleAsync(),
            await Genre("Aventura"), await Genre("Carreras"), await Platform("PC"), await Platform("Consola"),
            await Country("DO"), await Country("MX"),
            distributions[0], distributions[1]);
    });
}
