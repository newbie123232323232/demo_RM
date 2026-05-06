using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RevenueModule.Api.Tests;

public sealed class Step6IntegrationTests
{
    [Fact]
    public async Task ConfirmThenComplete_UpdatesBillAndBuyerVipPoint()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var buyerCreateResp = await client.PostAsJsonAsync("/api/buyers", new
        {
            name = "it-step6-buyer-" + Guid.NewGuid().ToString("N")[..8],
            initialVipPoint = 25,
        });
        buyerCreateResp.EnsureSuccessStatusCode();
        var buyerBefore = await buyerCreateResp.Content.ReadFromJsonAsync<BuyerDto>();
        Assert.NotNull(buyerBefore);

        var confirmPayload = new
        {
            buyerId = buyerBefore!.Id,
            lines = new[]
            {
                new { productId = 1, qty = 1, unitPriceVnd = 45_000L },
            },
            voucherType = "none",
            voucherValue = 0,
            vipPointsUsed = 5,
        };

        var confirmResp = await client.PostAsJsonAsync("/api/bills/confirm", confirmPayload);
        confirmResp.EnsureSuccessStatusCode();
        var confirm = await confirmResp.Content.ReadFromJsonAsync<ConfirmDto>();
        Assert.NotNull(confirm);
        Assert.Equal("Pending", confirm.Status);

        var completeResp = await client.PostAsync($"/api/bills/{confirm.BillId}/complete", null);
        completeResp.EnsureSuccessStatusCode();
        var complete = await completeResp.Content.ReadFromJsonAsync<CompleteDto>();
        Assert.NotNull(complete);
        Assert.Equal("Completed", complete.Status);

        var buyerAfter = await client.GetFromJsonAsync<BuyerDto>($"/api/buyers/{buyerBefore.Id}");
        Assert.NotNull(buyerAfter);
        var expectedPoint = buyerBefore!.VipPoint - 5 + (int)complete.VipPointEarned;
        Assert.Equal(expectedPoint, buyerAfter!.VipPoint);
    }

    [Fact]
    public async Task BillsListFilter_StatusCompletedOnly_ReturnsOnlyCompletedRows()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var buyerCreateResp = await client.PostAsJsonAsync("/api/buyers", new
        {
            name = "it-step6-buyer-" + Guid.NewGuid().ToString("N")[..8],
            initialVipPoint = 10,
        });
        buyerCreateResp.EnsureSuccessStatusCode();
        var buyer = await buyerCreateResp.Content.ReadFromJsonAsync<BuyerDto>();
        Assert.NotNull(buyer);

        var pendingPayload = new
        {
            buyerId = buyer!.Id,
            lines = new[] { new { productId = 1, qty = 1, unitPriceVnd = 45_000L } },
            voucherType = "none",
            voucherValue = 0,
            vipPointsUsed = 0,
        };
        var pendingResp = await client.PostAsJsonAsync("/api/bills/confirm", pendingPayload);
        pendingResp.EnsureSuccessStatusCode();

        var completedResp = await client.PostAsJsonAsync("/api/bills/confirm", pendingPayload);
        completedResp.EnsureSuccessStatusCode();
        var completed = await completedResp.Content.ReadFromJsonAsync<ConfirmDto>();
        Assert.NotNull(completed);
        var completeResp = await client.PostAsync($"/api/bills/{completed!.BillId}/complete", null);
        completeResp.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<BillListResponseDto>(
            "/api/bills?status=completed&timeField=completed&page=1&pageSize=20");
        Assert.NotNull(list);
        Assert.NotEmpty(list!.Items);
        Assert.All(list.Items, x => Assert.Equal("Completed", x.Status));
    }

    [Fact]
    public async Task RevenueSeries_UsesCompletedBillsOnly()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var buyerCreateResp = await client.PostAsJsonAsync("/api/buyers", new
        {
            name = "it-step6-buyer-" + Guid.NewGuid().ToString("N")[..8],
            initialVipPoint = 10,
        });
        buyerCreateResp.EnsureSuccessStatusCode();
        var buyer = await buyerCreateResp.Content.ReadFromJsonAsync<BuyerDto>();
        Assert.NotNull(buyer);

        var payload = new
        {
            buyerId = buyer!.Id,
            lines = new[] { new { productId = 1, qty = 1, unitPriceVnd = 45_000L } },
            voucherType = "none",
            voucherValue = 0,
            vipPointsUsed = 0,
        };

        var pendingResp = await client.PostAsJsonAsync("/api/bills/confirm", payload);
        pendingResp.EnsureSuccessStatusCode();
        var pending = await pendingResp.Content.ReadFromJsonAsync<ConfirmDto>();
        Assert.NotNull(pending);

        var completedResp = await client.PostAsJsonAsync("/api/bills/confirm", payload);
        completedResp.EnsureSuccessStatusCode();
        var completed = await completedResp.Content.ReadFromJsonAsync<ConfirmDto>();
        Assert.NotNull(completed);
        await client.PostAsync($"/api/bills/{completed!.BillId}/complete", null);

        var series = await client.GetFromJsonAsync<RevenueSeriesDto>(
            $"/api/reports/revenue-series?mode=byBuyer&buyerId={buyer.Id}&bucket=day");
        Assert.NotNull(series);
        var totalRevenue = series!.Points.Sum(x => x.RevenueVnd);

        Assert.Equal(completed.Payable, totalRevenue);
        Assert.NotEqual(pending!.Payable + completed.Payable, totalRevenue);
    }

    [Fact]
    public async Task RevenueSeries_WithFromAndTo_FillsZeroForEmptyCalendarBuckets()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var buyerCreateResp = await client.PostAsJsonAsync("/api/buyers", new
        {
            name = "it-dense-" + Guid.NewGuid().ToString("N")[..8],
            initialVipPoint = 10,
        });
        buyerCreateResp.EnsureSuccessStatusCode();
        var buyer = await buyerCreateResp.Content.ReadFromJsonAsync<BuyerDto>();
        Assert.NotNull(buyer);

        var payload = new
        {
            buyerId = buyer!.Id,
            lines = new[] { new { productId = 1, qty = 1, unitPriceVnd = 45_000L } },
            voucherType = "none",
            voucherValue = 0,
            vipPointsUsed = 0,
        };

        var completedResp = await client.PostAsJsonAsync("/api/bills/confirm", payload);
        completedResp.EnsureSuccessStatusCode();
        var completed = await completedResp.Content.ReadFromJsonAsync<ConfirmDto>();
        Assert.NotNull(completed);
        await client.PostAsync($"/api/bills/{completed!.BillId}/complete", null);

        var backdateResp = await client.PostAsJsonAsync(
            $"/api/bills/{completed.BillId}/test-backdate",
            new
            {
                pendingAtUtc = new DateTime(2020, 6, 1, 2, 0, 0, DateTimeKind.Utc),
                completedAtUtc = new DateTime(2020, 6, 1, 3, 0, 0, DateTimeKind.Utc),
            });
        backdateResp.EnsureSuccessStatusCode();

        var series = await client.GetFromJsonAsync<RevenueSeriesDto>(
            $"/api/reports/revenue-series?mode=byBuyer&buyerId={buyer.Id}&bucket=day&from=2020-06-01&to=2020-06-03");
        Assert.NotNull(series);
        Assert.Equal(3, series!.Points.Count);
        Assert.Equal(completed.Payable, series.Points[0].RevenueVnd);
        Assert.Equal(0, series.Points[1].RevenueVnd);
        Assert.Equal(0, series.Points[2].RevenueVnd);
    }

    [Fact]
    public async Task RevenueSeries_InvalidDateRange_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/reports/revenue-series?mode=total&bucket=day&from=2020-06-10&to=2020-06-01");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BillsList_DateOnlyRange_DoesNot500_AndReturnsFilteredRows()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var buyerCreateResp = await client.PostAsJsonAsync("/api/buyers", new
        {
            name = "it-bills-range-" + Guid.NewGuid().ToString("N")[..8],
            initialVipPoint = 0,
        });
        buyerCreateResp.EnsureSuccessStatusCode();
        var buyer = await buyerCreateResp.Content.ReadFromJsonAsync<BuyerDto>();
        Assert.NotNull(buyer);

        var payload = new
        {
            buyerId = buyer!.Id,
            lines = new[] { new { productId = 1, qty = 1, unitPriceVnd = 45_000L } },
            voucherType = "none",
            voucherValue = 0,
            vipPointsUsed = 0,
        };

        var confirmResp = await client.PostAsJsonAsync("/api/bills/confirm", payload);
        confirmResp.EnsureSuccessStatusCode();
        var confirm = await confirmResp.Content.ReadFromJsonAsync<ConfirmDto>();
        Assert.NotNull(confirm);
        var completeResp = await client.PostAsync($"/api/bills/{confirm!.BillId}/complete", null);
        completeResp.EnsureSuccessStatusCode();

        var backdateResp = await client.PostAsJsonAsync(
            $"/api/bills/{confirm.BillId}/test-backdate",
            new
            {
                pendingAtUtc = new DateTime(2020, 6, 2, 1, 0, 0, DateTimeKind.Utc),
                completedAtUtc = new DateTime(2020, 6, 2, 2, 0, 0, DateTimeKind.Utc),
            });
        backdateResp.EnsureSuccessStatusCode();

        var listResp = await client.GetFromJsonAsync<BillListResponseDto>(
            "/api/bills?status=completed&timeField=pending&from=2020-06-01&to=2020-06-03&page=1&pageSize=20");

        Assert.NotNull(listResp);
        Assert.True(listResp!.Items.Count >= 1);
        Assert.Contains(listResp.Items, x => x.BillId == confirm.BillId);
    }

    [Fact]
    public async Task BillsCsv_DateOnlyRange_DoesNot500_AndReturnsCsvBody()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/bills/export.csv?from=2020-06-01&to=2020-06-03");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("BillId,BuyerId,Status", body);
    }

    [Fact]
    public async Task BillsList_InvalidDateRange_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/bills?from=2020-06-10&to=2020-06-01");
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed class RevenueApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var testConn = Environment.GetEnvironmentVariable("TEST_REVENUE_DB")
                    ?? "Host=localhost;Port=5432;Database=HMModule;Username=hmuser;Password=123456";
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:RevenueDb"] = testConn,
                });
            });
        }
    }

    private sealed record BuyerDto(int Id, string Name, int VipPoint);
    private sealed record ConfirmDto(long BillId, string Status, DateTime PendingAtUtc, long Subtotal, long VoucherThuongAmount, string VoucherType, decimal VoucherValue, long BaseBeforeVip, int VipPointUsed, long VipDiscountVnd, long Payable);
    private sealed record CompleteDto(long BillId, string Status, DateTime? CompletedAtUtc, long Payable, long VipPointEarned);
    private sealed record BillListResponseDto(IReadOnlyList<BillListItemDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
    private sealed record BillListItemDto(long BillId, int BuyerId, string Status, DateTime PendingAtUtc, DateTime? CompletedAtUtc, long Subtotal, long VoucherThuongAmount, string VoucherType, decimal VoucherValue, long BaseBeforeVip, int VipPointUsed, long VipDiscountVnd, long Payable, long VipPointEarned);
    private sealed record RevenueSeriesDto(string Mode, string Bucket, IReadOnlyList<RevenuePointDto> Points);
    private sealed record RevenuePointDto(DateTime BucketStartLocal, long RevenueVnd);
}
