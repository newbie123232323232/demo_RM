using RevenueModule.Api.Services;

namespace RevenueModule.Api.Tests;

public class BillPreviewCalculatorTests
{
    private readonly BillPreviewCalculator _calculator = new();

    [Fact]
    public void Calculate_WithPercentVoucherAndVipCap_ReturnsExpectedValues()
    {
        var result = _calculator.Calculate(new PreviewCalculationInput(
            [
                new PreviewLineInput(1_000_000, 10), // 10,000,000
            ],
            VoucherType.Percent,
            5,
            50));

        Assert.Equal(10_000_000, result.Subtotal);
        Assert.Equal(500_000, result.VoucherThuongAmount);
        Assert.Equal(9_500_000, result.BaseBeforeVip);
        Assert.Equal(9_500_000 * 50 / 100, result.VipDiscount); // percent smaller than cap here
        Assert.Equal(4_750_000, result.Payable);
        Assert.Equal(4, result.ExpectedVipPointsEarnedIfCompleted);
    }

    [Fact]
    public void Calculate_WithFixedVoucherAndVipCapHit_UsesCap()
    {
        var result = _calculator.Calculate(new PreviewCalculationInput(
            [
                new PreviewLineInput(10_000_000, 3), // 30,000,000
            ],
            VoucherType.FixedVnd,
            1_000_000,
            5));

        Assert.Equal(30_000_000, result.Subtotal);
        Assert.Equal(1_000_000, result.VoucherThuongAmount);
        Assert.Equal(29_000_000, result.BaseBeforeVip);
        Assert.Equal(1_000_000, result.VipDiscount); // 5% = 1.45M, cap = 1.0M
        Assert.Equal(28_000_000, result.Payable);
        Assert.Equal(28, result.ExpectedVipPointsEarnedIfCompleted);
    }

    [Fact]
    public void Calculate_WithVipPointsZero_HasNoVipDiscount()
    {
        var result = _calculator.Calculate(new PreviewCalculationInput(
            [
                new PreviewLineInput(2_450_000, 1),
            ],
            VoucherType.None,
            0,
            0));

        Assert.Equal(2_450_000, result.Subtotal);
        Assert.Equal(0, result.VoucherThuongAmount);
        Assert.Equal(0, result.VipDiscount);
        Assert.Equal(2_450_000, result.Payable);
        Assert.Equal(2, result.ExpectedVipPointsEarnedIfCompleted);
    }
}
