using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Data;

namespace RevenueModule.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(RevenueDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ProductListItemDto(x.Id, x.Name, x.UnitPriceVnd))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    public sealed record ProductListItemDto(int Id, string Name, long UnitPriceVnd);
}
