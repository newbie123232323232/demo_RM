namespace RevenueModule.Api.Services;

public sealed class BillPreviewCalculator
{
    public PreviewCalculationResult Calculate(PreviewCalculationInput input)
    {
        var subtotal = input.Lines.Sum(x => x.UnitPriceVnd * x.Qty);

        var voucherAmount = input.VoucherType switch
        {
            VoucherType.None => 0L,
            VoucherType.Percent => (long)Math.Floor(subtotal * (input.VoucherValue / 100m)),
            VoucherType.FixedVnd => (long)Math.Floor(input.VoucherValue),
            _ => 0L,
        };
        voucherAmount = Math.Clamp(voucherAmount, 0, subtotal);

        var baseBeforeVip = subtotal - voucherAmount;
        var vipDiscount = 0L;

        if (input.VipPointsUsed > 0)
        {
            var byPercent = (long)Math.Floor(baseBeforeVip * (input.VipPointsUsed / 100m));
            var byCap = input.VipPointsUsed * 200_000L;
            vipDiscount = Math.Min(byPercent, byCap);
            vipDiscount = Math.Clamp(vipDiscount, 0, baseBeforeVip);
        }

        var payable = baseBeforeVip - vipDiscount;
        var expectedEarned = payable / 1_000_000;

        return new PreviewCalculationResult(
            subtotal,
            voucherAmount,
            baseBeforeVip,
            vipDiscount,
            payable,
            expectedEarned);
    }
}

public sealed record PreviewCalculationInput(
    IReadOnlyList<PreviewLineInput> Lines,
    VoucherType VoucherType,
    decimal VoucherValue,
    int VipPointsUsed);

public sealed record PreviewLineInput(long UnitPriceVnd, int Qty);

public sealed record PreviewCalculationResult(
    long Subtotal,
    long VoucherThuongAmount,
    long BaseBeforeVip,
    long VipDiscount,
    long Payable,
    long ExpectedVipPointsEarnedIfCompleted);

public enum VoucherType
{
    None,
    Percent,
    FixedVnd,
}
