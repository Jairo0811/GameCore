namespace GameCore.Domain.Entities;

public sealed class Customer
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
