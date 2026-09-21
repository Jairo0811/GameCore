namespace GameCore.Domain.Entities;

public sealed class Employee
{
    public int EmployeeId { get; set; }
    public int BranchId { get; set; }
    public int JobPositionId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Branch Branch { get; set; } = null!;
    public JobPosition JobPosition { get; set; } = null!;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
