namespace GameCore.Domain.Entities;

public sealed class Sale
{
    public long SaleId { get; set; }
    public int CustomerId { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime SaleDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;
    public Employee? Employee { get; set; }
    public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
}
