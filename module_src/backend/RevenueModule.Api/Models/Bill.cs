namespace RevenueModule.Api.Models;

public sealed class Bill
{
    public long Id { get; set; }
    public int BuyerId { get; set; }
    public Buyer Buyer { get; set; } = null!;

    public string Status { get; set; } = BillStatus.Pending;
    public DateTime PendingAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public long Subtotal { get; set; }
    public long VoucherThuongAmount { get; set; }
    public string VoucherType { get; set; } = "none";
    public decimal VoucherValue { get; set; }
    public long BaseBeforeVip { get; set; }
    public int VipPointUsed { get; set; }
    public long VipDiscountVnd { get; set; }
    public long Payable { get; set; }
    public long VipPointEarned { get; set; }

    public List<BillLine> Lines { get; set; } = [];
}

public static class BillStatus
{
    public const string Pending = "Pending";
    public const string Completed = "Completed";
}
