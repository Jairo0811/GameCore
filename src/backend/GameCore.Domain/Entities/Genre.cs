namespace GameCore.Domain.Entities;

public sealed class Genre
{
    public int GenreId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
}
