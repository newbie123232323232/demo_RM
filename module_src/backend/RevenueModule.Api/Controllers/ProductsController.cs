using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Data;
using RevenueModule.Api.Models;

namespace RevenueModule.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(RevenueDbContext dbContext) : ControllerBase
{
    private const int NameMaxLength = 160;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    /// <summary>
    /// List products. Backward-compat: when no query is provided, returns a flat array
    /// (consumed by /lab/bill and /lab/revenue). When any of q/priceMin/priceMax/page/pageSize
    /// is set, returns a paged response (consumed by the new admin /lab/products page).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? q,
        [FromQuery] long? priceMin,
        [FromQuery] long? priceMax,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (priceMin.HasValue && priceMax.HasValue && priceMin.Value > priceMax.Value)
        {
            return BadRequest(new ApiError("invalid_price_range", "priceMin phải nhỏ hơn hoặc bằng priceMax."));
        }

        var hasFilterParam = !string.IsNullOrWhiteSpace(q)
                             || priceMin.HasValue
                             || priceMax.HasValue
                             || page.HasValue
                             || pageSize.HasValue;

        var query = dbContext.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var needle = q.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{needle}%"));
        }

        if (priceMin.HasValue)
        {
            var min = priceMin.Value;
            query = query.Where(x => x.UnitPriceVnd >= min);
        }

        if (priceMax.HasValue)
        {
            var max = priceMax.Value;
            query = query.Where(x => x.UnitPriceVnd <= max);
        }

        query = query.OrderBy(x => x.Id);

        if (!hasFilterParam)
        {
            var allItems = await query
                .Select(x => new ProductListItemDto(x.Id, x.Name, x.UnitPriceVnd))
                .ToListAsync(cancellationToken);
            return Ok(allItems);
        }

        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            normalizedPage = 1;
        }

        var normalizedPageSize = NormalizePageSize(pageSize);
        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)normalizedPageSize);
        if (totalPages > 0 && normalizedPage > totalPages)
        {
            normalizedPage = totalPages;
        }

        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new ProductListItemDto(x.Id, x.Name, x.UnitPriceVnd))
            .ToListAsync(cancellationToken);

        return Ok(new ProductListResponse(items, normalizedPage, normalizedPageSize, totalCount, totalPages));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductListItemDto(x.Id, x.Name, x.UnitPriceVnd))
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            return NotFound(new ApiError("product_not_found", "Sản phẩm không tồn tại hoặc đã bị xoá."));
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductWriteRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateProductPayload(request);
        if (validation is not null)
        {
            return validation;
        }

        var product = new Product
        {
            Name = request.Name!.Trim(),
            UnitPriceVnd = request.UnitPriceVnd,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ProductListItemDto(product.Id, product.Name, product.UnitPriceVnd);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductWriteRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateProductPayload(request);
        if (validation is not null)
        {
            return validation;
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(new ApiError("product_not_found", "Sản phẩm không tồn tại hoặc đã bị xoá."));
        }

        product.Name = request.Name!.Trim();
        product.UnitPriceVnd = request.UnitPriceVnd;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ProductListItemDto(product.Id, product.Name, product.UnitPriceVnd));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(new ApiError("product_not_found", "Sản phẩm không tồn tại hoặc đã bị xoá."));
        }

        var inUse = await dbContext.BillLines
            .AsNoTracking()
            .AnyAsync(x => x.ProductId == id, cancellationToken);
        if (inUse)
        {
            return Conflict(new ApiError(
                "conflict_product_in_use",
                "Sản phẩm đang được dùng trong bill, không thể xoá."));
        }

        try
        {
            dbContext.Products.Remove(product);
            await dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException)
        {
            // Defense-in-depth: nếu một bill được tạo giữa lúc check Any() và SaveChanges(),
            // FK Restrict ở DB sẽ ném exception. Map sang cùng 409 contract để client xử lý nhất quán.
            return Conflict(new ApiError(
                "conflict_product_in_use",
                "Sản phẩm đang được dùng trong bill, không thể xoá."));
        }
    }

    private IActionResult? ValidateProductPayload(ProductWriteRequest? request)
    {
        if (request is null)
        {
            return BadRequest(new ApiError("invalid_payload", "Thiếu nội dung request."));
        }

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest(new ApiError("invalid_payload", "Tên sản phẩm không được để trống."));
        }

        if (name.Length > NameMaxLength)
        {
            return BadRequest(new ApiError("invalid_payload", $"Tên sản phẩm tối đa {NameMaxLength} ký tự."));
        }

        if (request.UnitPriceVnd <= 0)
        {
            return BadRequest(new ApiError("invalid_payload", "Đơn giá phải > 0 VND."));
        }

        return null;
    }

    private static int NormalizePageSize(int? pageSize)
    {
        var value = pageSize.GetValueOrDefault(DefaultPageSize);
        if (value <= 0)
        {
            return DefaultPageSize;
        }
        return value > MaxPageSize ? MaxPageSize : value;
    }

    public sealed record ProductListItemDto(int Id, string Name, long UnitPriceVnd);

    public sealed record ProductListResponse(
        IReadOnlyList<ProductListItemDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    public sealed record ProductWriteRequest(string? Name, long UnitPriceVnd);
}
