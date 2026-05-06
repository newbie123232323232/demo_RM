namespace RevenueModule.Api.Models;

public sealed class BillLine
{
    public long Id { get; set; }
    public long BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public int ProductId { get; set; }
    public int Qty { get; set; }
    public long UnitPriceVnd { get; set; }
    public long LineTotalVnd { get; set; }
}
