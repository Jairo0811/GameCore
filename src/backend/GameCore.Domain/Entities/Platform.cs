namespace GameCore.Domain.Entities;

public sealed class Platform
{
    public int PlatformId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<GamePlatform> GamePlatforms { get; set; } = new List<GamePlatform>();
}
