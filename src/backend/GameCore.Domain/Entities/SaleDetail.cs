namespace GameCore.Domain.Entities;

public sealed class SaleDetail
{
    public long SaleDetailId { get; set; }
    public long SaleId { get; set; }
    public int GameId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Sale Sale { get; set; } = null!;
    public Game Game { get; set; } = null!;
}
