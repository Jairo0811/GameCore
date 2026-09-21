namespace GameCore.Domain.Entities;

public sealed class Distribution
{
    public long DistributionId { get; set; }
    public int GameId { get; set; }
    public int CountryId { get; set; }
    public DateOnly DistributionDate { get; set; }
    public int Units { get; set; }

    public Game Game { get; set; } = null!;
    public Country Country { get; set; } = null!;
}
