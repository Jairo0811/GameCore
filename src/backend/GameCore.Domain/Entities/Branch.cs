namespace GameCore.Domain.Entities;

public sealed class Branch
{
    public int BranchId { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AddressLine { get; set; }
    public bool IsActive { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
