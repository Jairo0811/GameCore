namespace GameCore.Domain.Entities;

public sealed class JobPosition
{
    public int JobPositionId { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
