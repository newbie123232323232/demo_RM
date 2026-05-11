using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Data;
using RevenueModule.Api.Models;

namespace RevenueModule.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(RevenueDbContext dbContext) : ControllerBase
{
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

        var hasQueryParams = q is not null
            || priceMin.HasValue
            || priceMax.HasValue
            || page.HasValue
            || pageSize.HasValue;

        var query = dbContext.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%"));
        }

        if (priceMin.HasValue)
        {
            query = query.Where(x => x.UnitPriceVnd >= priceMin.Value);
        }

        if (priceMax.HasValue)
        {
            query = query.Where(x => x.UnitPriceVnd <= priceMax.Value);
        }

        query = query.OrderBy(x => x.Id);

        if (!hasQueryParams)
        {
            var products = await query
                .Select(x => new ProductListItemDto(x.Id, x.Name, x.UnitPriceVnd))
                .ToListAsync(cancellationToken);
            return Ok(products);
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

        return Ok(new ProductListResponse(
            items,
            normalizedPage,
            normalizedPageSize,
            totalCount,
            totalPages));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductListItemDto(x.Id, x.Name, x.UnitPriceVnd))
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return NotFound(new ApiError("product_not_found", "Sản phẩm không tồn tại hoặc đã bị xoá."));
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertProductRequest? request, CancellationToken cancellationToken)
    {
        var validationError = ValidateProductPayload(request);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var normalizedName = request!.Name.Trim();
        var product = new Product
        {
            Name = normalizedName,
            UnitPriceVnd = request.UnitPriceVnd,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ProductListItemDto(product.Id, product.Name, product.UnitPriceVnd);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertProductRequest? request, CancellationToken cancellationToken)
    {
        var validationError = ValidateProductPayload(request);
        if (validationError is not null)
        {
            return BadRequest(validationError);
        }

        var product = await dbContext.Products.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound(new ApiError("product_not_found", "Sản phẩm không tồn tại hoặc đã bị xoá."));
        }

        product.Name = request!.Name.Trim();
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
            return Conflict(new ApiError("conflict_product_in_use", "Sản phẩm đang được dùng trong bill, không thể xoá."));
        }

        try
        {
            dbContext.Products.Remove(product);
            await dbContext.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new ApiError("conflict_product_in_use", "Sản phẩm đang được dùng trong bill, không thể xoá."));
        }
    }

    private static ApiError? ValidateProductPayload(UpsertProductRequest? request)
    {
        if (request is null)
        {
            return new ApiError("invalid_payload", "Thiếu nội dung request.");
        }

        var normalizedName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new ApiError("invalid_payload", "Tên sản phẩm không được để trống.");
        }

        if (normalizedName.Length > 160)
        {
            return new ApiError("invalid_payload", "Tên sản phẩm tối đa 160 ký tự.");
        }

        if (request.UnitPriceVnd <= 0)
        {
            return new ApiError("invalid_payload", "Đơn giá phải > 0 VND.");
        }

        return null;
    }

    private static int NormalizePageSize(int? pageSize)
    {
        var normalized = pageSize.GetValueOrDefault(20);
        if (normalized <= 0)
        {
            normalized = 20;
        }

        return Math.Min(normalized, 100);
    }

    public sealed record UpsertProductRequest(string Name, long UnitPriceVnd);
    public sealed record ProductListItemDto(int Id, string Name, long UnitPriceVnd);
    public sealed record ProductListResponse(
        IReadOnlyList<ProductListItemDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}
