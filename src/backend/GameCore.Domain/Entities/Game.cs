namespace GameCore.Domain.Entities;

public sealed class Game
{
    public int GameId { get; set; }
    public int? AgeRatingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Story { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public AgeRating? AgeRating { get; set; }
    public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
    public ICollection<GamePlatform> GamePlatforms { get; set; } = new List<GamePlatform>();
    public ICollection<Distribution> Distributions { get; set; } = new List<Distribution>();
    public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
}
