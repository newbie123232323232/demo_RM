using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Data;
using RevenueModule.Api.Models;

namespace RevenueModule.Api.Controllers;

[ApiController]
[Route("api/buyers")]
public sealed class BuyersController(RevenueDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BuyerListItemDto>>> GetAll(CancellationToken cancellationToken)
    {
        var buyers = await dbContext.Buyers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new BuyerListItemDto(x.Id, x.Name, x.VipPoint))
            .ToListAsync(cancellationToken);

        return Ok(buyers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BuyerDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var buyer = await dbContext.Buyers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new BuyerDetailDto(x.Id, x.Name, x.VipPoint))
            .SingleOrDefaultAsync(cancellationToken);

        return buyer is null
            ? NotFound(new ApiError("buyer_not_found", "Buyer not found."))
            : Ok(buyer);
    }

    [HttpPost]
    public async Task<ActionResult<BuyerDetailDto>> Create([FromBody] CreateBuyerRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return BadRequest(new ApiError("invalid_name", "Name is required."));
        }

        if (request.InitialVipPoint < 0)
        {
            return BadRequest(new ApiError("invalid_initial_vip_point", "InitialVipPoint must be >= 0."));
        }

        var buyer = new Models.Buyer
        {
            Name = normalizedName,
            VipPoint = request.InitialVipPoint,
        };

        dbContext.Buyers.Add(buyer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = buyer.Id },
            new BuyerDetailDto(buyer.Id, buyer.Name, buyer.VipPoint));
    }

    public sealed record BuyerListItemDto(int Id, string Name, int VipPoint);
    public sealed record BuyerDetailDto(int Id, string Name, int VipPoint);
    public sealed record CreateBuyerRequest(string Name, int InitialVipPoint = 0);
}
