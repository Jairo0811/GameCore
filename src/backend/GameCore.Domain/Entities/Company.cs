namespace GameCore.Domain.Entities;

public sealed class Company
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
