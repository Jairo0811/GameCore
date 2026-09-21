namespace GameCore.Domain.Entities;

public sealed class GameGenre
{
    public int GameId { get; set; }
    public int GenreId { get; set; }

    public Game Game { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
