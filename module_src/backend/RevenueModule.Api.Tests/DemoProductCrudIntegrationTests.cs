using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RevenueModule.Api.Tests;

public sealed class DemoProductCrudIntegrationTests
{
    [Fact]
    public async Task CreateProduct_HappyPath_Returns201WithBody()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"it-product-{suffix}",
            unitPriceVnd = 150_000L,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(body);
        Assert.True(body!.Id > 0);
        Assert.Equal($"it-product-{suffix}", body.Name);
        Assert.Equal(150_000L, body.UnitPriceVnd);
        var location = response.Headers.Location!.ToString();
        Assert.Contains($"/api/products/{body.Id}", location, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProduct_EmptyName_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "   ",
            unitPriceVnd = 10_000L,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("invalid_payload", error!.Code);
    }

    [Fact]
    public async Task CreateProduct_NullName_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = (string?)null,
            unitPriceVnd = 10_000L,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_NameOver160Chars_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = new string('a', 161),
            unitPriceVnd = 10_000L,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("invalid_payload", error!.Code);
    }

    [Fact]
    public async Task CreateProduct_PriceZeroOrNegative_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var zeroResponse = await client.PostAsJsonAsync("/api/products", new
        {
            name = "it-invalid-price-zero",
            unitPriceVnd = 0L,
        });
        Assert.Equal(HttpStatusCode.BadRequest, zeroResponse.StatusCode);

        var negativeResponse = await client.PostAsJsonAsync("/api/products", new
        {
            name = "it-invalid-price-negative",
            unitPriceVnd = -1L,
        });
        Assert.Equal(HttpStatusCode.BadRequest, negativeResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_HappyPath_Returns200()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var created = await CreateProduct(client, "it-update");
        var response = await client.PutAsJsonAsync($"/api/products/{created.Id}", new
        {
            name = "it-update-new",
            unitPriceVnd = 777_000L,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated!.Id);
        Assert.Equal("it-update-new", updated.Name);
        Assert.Equal(777_000L, updated.UnitPriceVnd);
    }

    [Fact]
    public async Task UpdateProduct_NotFound_Returns404()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/products/999999", new
        {
            name = "not-found",
            unitPriceVnd = 12_000L,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("product_not_found", error!.Code);
    }

    [Fact]
    public async Task DeleteProduct_HappyPath_Returns204()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var created = await CreateProduct(client, "it-delete");
        var response = await client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WhenReferencedByBill_Returns409()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var buyerSuffix = Guid.NewGuid().ToString("N")[..8];
        var buyerResponse = await client.PostAsJsonAsync("/api/buyers", new
        {
            name = $"it-buyer-{buyerSuffix}",
            initialVipPoint = 0,
        });
        buyerResponse.EnsureSuccessStatusCode();
        var buyer = await buyerResponse.Content.ReadFromJsonAsync<BuyerDto>();
        Assert.NotNull(buyer);

        var confirmResponse = await client.PostAsJsonAsync("/api/bills/confirm", new
        {
            buyerId = buyer!.Id,
            lines = new[]
            {
                new { productId = 1, qty = 1, unitPriceVnd = 45_000L },
            },
            voucherType = "none",
            voucherValue = 0,
            vipPointsUsed = 0,
        });
        confirmResponse.EnsureSuccessStatusCode();

        var deleteResponse = await client.DeleteAsync("/api/products/1");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var error = await deleteResponse.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("conflict_product_in_use", error!.Code);
    }

    [Fact]
    public async Task GetProductsList_WithFilter_PagesAndSearches()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        await CreateProduct(client, $"it-list-{suffix}-a", 111_000L);
        await CreateProduct(client, $"it-list-{suffix}-b", 333_000L);

        var response = await client.GetAsync($"/api/products?q=it-list-{suffix}&priceMin=100000&priceMax=200000&page=1&pageSize=1");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ProductListResponseDto>();

        Assert.NotNull(body);
        Assert.Equal(1, body!.Page);
        Assert.Equal(1, body.PageSize);
        Assert.True(body.TotalCount >= 1);
        Assert.True(body.TotalPages >= 1);
        Assert.Single(body.Items);
        Assert.Contains($"it-list-{suffix}", body.Items[0].Name);
    }

    [Fact]
    public async Task GetProductsList_NoQuery_StillReturnsArrayShape()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(raw);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetProductsList_PriceMinGreaterThanPriceMax_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products?priceMin=200&priceMax=100");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.NotNull(error);
        Assert.Equal("invalid_price_range", error!.Code);
    }

    [Fact]
    public async Task GetProductsList_PageSizeOver100_ClampsTo100()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products?page=1&pageSize=999");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ProductListResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(100, body!.PageSize);
    }

    private static async Task<ProductDto> CreateProduct(HttpClient client, string prefix, long unitPrice = 120_000L)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"{prefix}-{suffix}",
            unitPriceVnd = unitPrice,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(body);
        return body!;
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

    private sealed record ProductDto(int Id, string Name, long UnitPriceVnd);
    private sealed record ProductListResponseDto(IReadOnlyList<ProductDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
    private sealed record ApiErrorDto(string Code, string Message);
    private sealed record BuyerDto(int Id, string Name, int VipPoint);
}
