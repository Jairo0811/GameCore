namespace GameCore.Domain.Entities;

public sealed class GamePlatform
{
    public int GameId { get; set; }
    public int PlatformId { get; set; }

    public Game Game { get; set; } = null!;
    public Platform Platform { get; set; } = null!;
}
