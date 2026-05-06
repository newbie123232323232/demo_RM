namespace RevenueModule.Api.Models;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long UnitPriceVnd { get; set; }
}
