namespace RevenueModule.Api.Models;

public sealed class Buyer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int VipPoint { get; set; }
}
