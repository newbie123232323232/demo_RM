using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RevenueModule.Api.Tests;

/// <summary>
/// Integration tests cho ProductsController CRUD + filter + 409 conflict
/// (Demo "Add Product (Admin)" - Step D2).
/// </summary>
public sealed class DemoProductCrudIntegrationTests
{
    [Fact]
    public async Task CreateProduct_HappyPath_Returns201WithBody()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var name = UniqueProductName();
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name,
            unitPriceVnd = 123_000L,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(dto);
        Assert.True(dto!.Id > 0);
        Assert.Equal(name, dto.Name);
        Assert.Equal(123_000L, dto.UnitPriceVnd);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(dto.Id.ToString(), response.Headers.Location!.ToString());

        await client.DeleteAsync($"/api/products/{dto.Id}");
    }

    [Fact]
    public async Task CreateProduct_EmptyName_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "   ",
            unitPriceVnd = 1_000L,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Equal("invalid_payload", error?.Code);
    }

    [Fact]
    public async Task CreateProduct_ZeroOrNegativePrice_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var responseZero = await client.PostAsJsonAsync("/api/products", new
        {
            name = UniqueProductName(),
            unitPriceVnd = 0L,
        });
        Assert.Equal(HttpStatusCode.BadRequest, responseZero.StatusCode);

        var responseNeg = await client.PostAsJsonAsync("/api/products", new
        {
            name = UniqueProductName(),
            unitPriceVnd = -1L,
        });
        Assert.Equal(HttpStatusCode.BadRequest, responseNeg.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_NameOver160Chars_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var longName = new string('A', 161);
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = longName,
            unitPriceVnd = 1_000L,
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_HappyPath_Returns200()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var created = await CreateProduct(client, UniqueProductName(), 50_000L);
        try
        {
            var updateResp = await client.PutAsJsonAsync($"/api/products/{created.Id}", new
            {
                name = created.Name + "-edited",
                unitPriceVnd = 75_000L,
            });
            Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
            var updated = await updateResp.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(updated);
            Assert.Equal(created.Id, updated!.Id);
            Assert.Equal(created.Name + "-edited", updated.Name);
            Assert.Equal(75_000L, updated.UnitPriceVnd);
        }
        finally
        {
            await client.DeleteAsync($"/api/products/{created.Id}");
        }
    }

    [Fact]
    public async Task UpdateProduct_NotFound_Returns404()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/products/999999999", new
        {
            name = UniqueProductName(),
            unitPriceVnd = 1_000L,
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Equal("product_not_found", error?.Code);
    }

    [Fact]
    public async Task DeleteProduct_HappyPath_Returns204()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var created = await CreateProduct(client, UniqueProductName(), 50_000L);
        var deleteResp = await client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var getResp = await client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_NotFound_Returns404()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/products/999999998");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_WhenReferencedByBill_Returns409()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var product = await CreateProduct(client, UniqueProductName(), 60_000L);
        var buyer = await CreateBuyer(client);

        var confirmResp = await client.PostAsJsonAsync("/api/bills/confirm", new
        {
            buyerId = buyer.Id,
            lines = new[]
            {
                new { productId = product.Id, qty = 1, unitPriceVnd = product.UnitPriceVnd },
            },
            voucherType = "none",
            voucherValue = 0,
            vipPointsUsed = 0,
        });
        confirmResp.EnsureSuccessStatusCode();

        var deleteResp = await client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResp.StatusCode);
        var error = await deleteResp.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Equal("conflict_product_in_use", error?.Code);
    }

    [Fact]
    public async Task GetProductsList_NoQuery_ReturnsArrayShape_BackwardCompat()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.StartsWith("[", body.TrimStart());

        var arr = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(arr);
        Assert.NotEmpty(arr!);
    }

    [Fact]
    public async Task GetProductsList_WithFilter_ReturnsPagedShape_AndFiltersByNameAndPrice()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var tag = "demoFilter-" + Guid.NewGuid().ToString("N")[..8];
        var p1 = await CreateProduct(client, $"{tag}-cheap", 10_000L);
        var p2 = await CreateProduct(client, $"{tag}-mid", 100_000L);
        var p3 = await CreateProduct(client, $"{tag}-pricey", 1_000_000L);
        try
        {
            var response = await client.GetAsync($"/api/products?q={tag}&priceMin=50000&priceMax=500000&page=1&pageSize=10");
            response.EnsureSuccessStatusCode();
            var paged = await response.Content.ReadFromJsonAsync<ProductPagedDto>();
            Assert.NotNull(paged);
            Assert.Single(paged!.Items);
            Assert.Equal(p2.Id, paged.Items[0].Id);
            Assert.Equal(1, paged.TotalCount);
            Assert.Equal(1, paged.TotalPages);
            Assert.Equal(1, paged.Page);
            Assert.Equal(10, paged.PageSize);

            var allOfTag = await client.GetFromJsonAsync<ProductPagedDto>($"/api/products?q={tag}&page=1&pageSize=10");
            Assert.NotNull(allOfTag);
            Assert.Equal(3, allOfTag!.TotalCount);
        }
        finally
        {
            await client.DeleteAsync($"/api/products/{p1.Id}");
            await client.DeleteAsync($"/api/products/{p2.Id}");
            await client.DeleteAsync($"/api/products/{p3.Id}");
        }
    }

    [Fact]
    public async Task CreateProduct_NullName_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = (string?)null,
            unitPriceVnd = 1_000L,
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Equal("invalid_payload", error?.Code);
    }

    [Fact]
    public async Task GetProductsList_PriceMinGreaterThanPriceMax_Returns400()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products?priceMin=500000&priceMax=100000");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>();
        Assert.Equal("invalid_price_range", error?.Code);
    }

    [Fact]
    public async Task GetProductsList_PageSizeOver100_ClampsTo100()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var paged = await client.GetFromJsonAsync<ProductPagedDto>("/api/products?page=1&pageSize=99999");
        Assert.NotNull(paged);
        Assert.Equal(100, paged!.PageSize);
    }

    [Fact]
    public async Task GetProductsList_WithPaging_ClampsAndRespectsBounds()
    {
        await using var factory = new RevenueApiFactory();
        using var client = factory.CreateClient();

        var tag = "demoPage-" + Guid.NewGuid().ToString("N")[..8];
        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            var dto = await CreateProduct(client, $"{tag}-{i}", 1000L + i);
            ids.Add(dto.Id);
        }
        try
        {
            var pagedResp = await client.GetFromJsonAsync<ProductPagedDto>($"/api/products?q={tag}&page=1&pageSize=2");
            Assert.NotNull(pagedResp);
            Assert.Equal(2, pagedResp!.Items.Count);
            Assert.Equal(5, pagedResp.TotalCount);
            Assert.Equal(3, pagedResp.TotalPages);

            var lastPage = await client.GetFromJsonAsync<ProductPagedDto>($"/api/products?q={tag}&page=99&pageSize=2");
            Assert.NotNull(lastPage);
            Assert.Equal(3, lastPage!.Page);
            Assert.Single(lastPage.Items);
        }
        finally
        {
            foreach (var id in ids)
            {
                await client.DeleteAsync($"/api/products/{id}");
            }
        }
    }

    private static string UniqueProductName() => "demoProd-" + Guid.NewGuid().ToString("N")[..8];

    private static async Task<ProductDto> CreateProduct(HttpClient client, string name, long priceVnd)
    {
        var resp = await client.PostAsJsonAsync("/api/products", new { name, unitPriceVnd = priceVnd });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<BuyerDto> CreateBuyer(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/buyers", new
        {
            name = "demoBuyer-" + Guid.NewGuid().ToString("N")[..8],
            initialVipPoint = 0,
        });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<BuyerDto>();
        Assert.NotNull(dto);
        return dto!;
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
    private sealed record ProductPagedDto(IReadOnlyList<ProductDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
    private sealed record BuyerDto(int Id, string Name, int VipPoint);
    private sealed record ApiErrorDto(string Code, string Message);
}
