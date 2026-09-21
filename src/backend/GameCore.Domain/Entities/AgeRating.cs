namespace GameCore.Domain.Entities;

public sealed class AgeRating
{
    public int AgeRatingId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<Game> Games { get; set; } = new List<Game>();
}
