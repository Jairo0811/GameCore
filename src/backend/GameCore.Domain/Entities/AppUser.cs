namespace GameCore.Domain.Entities;

public sealed class AppUser
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Manager";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
