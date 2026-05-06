using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Data;
using RevenueModule.Api.Models;
using RevenueModule.Api.Services;
using System.Globalization;
using System.Text;

namespace RevenueModule.Api.Controllers;

[ApiController]
[Route("api/bills")]
public sealed class BillsController(
    RevenueDbContext dbContext,
    BillPreviewCalculator calculator,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int? buyerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? payableMaxVnd,
        [FromQuery] string? status,
        [FromQuery] string? bucket,
        [FromQuery] string? timeField,
        [FromQuery] List<int>? productIds,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var timeZone = ResolveVietnamTimeZone();
        if (from.HasValue && to.HasValue && VietnamCivilDateOnly(from.Value) > VietnamCivilDateOnly(to.Value))
        {
            return BadRequest(new ApiError("invalid_date_range", "from must be on or before to."));
        }

        var query = BuildBillFilterQuery(buyerId, from, to, payableMaxVnd, status, bucket, timeField, productIds, timeZone);
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
            .OrderByDescending(x => x.PendingAtUtc)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new BillListItemDto(
                x.Id,
                x.BuyerId,
                x.Status,
                x.PendingAtUtc,
                x.CompletedAtUtc,
                x.Subtotal,
                x.VoucherThuongAmount,
                x.VoucherType,
                x.VoucherValue,
                x.BaseBeforeVip,
                x.VipPointUsed,
                x.VipDiscountVnd,
                x.Payable,
                x.VipPointEarned))
            .ToListAsync(cancellationToken);

        return Ok(new BillListResponse(
            items,
            normalizedPage,
            normalizedPageSize,
            totalCount,
            totalPages));
    }

    [HttpGet("export.csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] int? buyerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? payableMaxVnd,
        [FromQuery] string? status,
        [FromQuery] string? bucket,
        [FromQuery] string? timeField,
        [FromQuery] List<int>? productIds,
        CancellationToken cancellationToken)
    {
        var timeZone = ResolveVietnamTimeZone();
        if (from.HasValue && to.HasValue && VietnamCivilDateOnly(from.Value) > VietnamCivilDateOnly(to.Value))
        {
            return BadRequest(new ApiError("invalid_date_range", "from must be on or before to."));
        }

        var rows = await BuildBillFilterQuery(buyerId, from, to, payableMaxVnd, status, bucket, timeField, productIds, timeZone)
            .OrderByDescending(x => x.PendingAtUtc)
            .Select(x => new BillCsvRowDto(
                x.Id,
                x.BuyerId,
                x.Status,
                x.PendingAtUtc,
                x.CompletedAtUtc,
                x.Subtotal,
                x.VoucherThuongAmount,
                x.VoucherType,
                x.VoucherValue,
                x.BaseBeforeVip,
                x.VipPointUsed,
                x.VipDiscountVnd,
                x.Payable,
                x.VipPointEarned))
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("BillId,BuyerId,Status,PendingAtUtc,CompletedAtUtc,Subtotal,VoucherThuongAmount,VoucherType,VoucherValue,BaseBeforeVip,VipPointUsed,VipDiscountVnd,Payable,VipPointEarned");
        foreach (var x in rows)
        {
            sb.AppendLine(
                string.Join(",",
                    x.BillId.ToString(CultureInfo.InvariantCulture),
                    x.BuyerId.ToString(CultureInfo.InvariantCulture),
                    x.Status,
                    x.PendingAtUtc.ToString("O", CultureInfo.InvariantCulture),
                    x.CompletedAtUtc.HasValue ? x.CompletedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture) : string.Empty,
                    x.Subtotal.ToString(CultureInfo.InvariantCulture),
                    x.VoucherThuongAmount.ToString(CultureInfo.InvariantCulture),
                    x.VoucherType,
                    x.VoucherValue.ToString(CultureInfo.InvariantCulture),
                    x.BaseBeforeVip.ToString(CultureInfo.InvariantCulture),
                    x.VipPointUsed.ToString(CultureInfo.InvariantCulture),
                    x.VipDiscountVnd.ToString(CultureInfo.InvariantCulture),
                    x.Payable.ToString(CultureInfo.InvariantCulture),
                    x.VipPointEarned.ToString(CultureInfo.InvariantCulture)));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "bills-export.csv");
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var calculation = CalculateFromRequest(request);

        return Ok(new PreviewResponse(
            calculation.Subtotal,
            calculation.VoucherThuongAmount,
            calculation.BaseBeforeVip,
            calculation.VipDiscount,
            calculation.Payable,
            calculation.ExpectedVipPointsEarnedIfCompleted));
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] PreviewRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateRequest(request, cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var buyer = await dbContext.Buyers.SingleAsync(x => x.Id == request.BuyerId, cancellationToken);
        var calculation = CalculateFromRequest(request);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (request.VipPointsUsed > 0)
        {
            if (buyer.VipPoint < request.VipPointsUsed)
            {
                return BadRequest(new ApiError("vip_balance_exceeded", "vipPointsUsed exceeds buyer balance."));
            }

            buyer.VipPoint -= request.VipPointsUsed;
        }

        var bill = new Bill
        {
            BuyerId = buyer.Id,
            Status = BillStatus.Pending,
            PendingAtUtc = DateTime.UtcNow,
            Subtotal = calculation.Subtotal,
            VoucherThuongAmount = calculation.VoucherThuongAmount,
            VoucherType = NormalizeVoucherType(request.VoucherType),
            VoucherValue = request.VoucherValue,
            BaseBeforeVip = calculation.BaseBeforeVip,
            VipPointUsed = request.VipPointsUsed,
            VipDiscountVnd = calculation.VipDiscount,
            Payable = calculation.Payable,
            VipPointEarned = 0,
            Lines = request.Lines.Select(x => new BillLine
            {
                ProductId = x.ProductId,
                Qty = x.Qty,
                UnitPriceVnd = x.UnitPriceVnd,
                LineTotalVnd = x.UnitPriceVnd * x.Qty,
            }).ToList(),
        };

        dbContext.Bills.Add(bill);
        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new ConfirmResponse(
            bill.Id,
            bill.Status,
            bill.PendingAtUtc,
            bill.Subtotal,
            bill.VoucherThuongAmount,
            bill.VoucherType,
            bill.VoucherValue,
            bill.BaseBeforeVip,
            bill.VipPointUsed,
            bill.VipDiscountVnd,
            bill.Payable));
    }

    [HttpPut("{id:long}/pending")]
    public async Task<IActionResult> UpdatePending(long id, [FromBody] PreviewRequest request, CancellationToken cancellationToken)
    {
        var bill = await dbContext.Bills
            .Include(x => x.Buyer)
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (bill is null)
        {
            return NotFound(new ApiError("bill_not_found", "Bill not found."));
        }

        if (bill.Status != BillStatus.Pending)
        {
            return BadRequest(new ApiError("bill_not_pending", "Only Pending bill can be updated."));
        }

        if (request.BuyerId != bill.BuyerId)
        {
            return BadRequest(new ApiError("buyer_immutable", "Cannot change buyer for existing Pending bill."));
        }

        var availableVip = bill.Buyer.VipPoint + bill.VipPointUsed;
        var validation = await ValidateRequest(request, cancellationToken, availableVip);
        if (validation is not null)
        {
            return validation;
        }

        var calculation = CalculateFromRequest(request);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Hoan tra so diem da tru truoc do, sau do tru lai theo request moi.
        bill.Buyer.VipPoint += bill.VipPointUsed;
        bill.Buyer.VipPoint -= request.VipPointsUsed;

        bill.Subtotal = calculation.Subtotal;
        bill.VoucherThuongAmount = calculation.VoucherThuongAmount;
        bill.VoucherType = NormalizeVoucherType(request.VoucherType);
        bill.VoucherValue = request.VoucherValue;
        bill.BaseBeforeVip = calculation.BaseBeforeVip;
        bill.VipPointUsed = request.VipPointsUsed;
        bill.VipDiscountVnd = calculation.VipDiscount;
        bill.Payable = calculation.Payable;

        dbContext.BillLines.RemoveRange(bill.Lines);
        bill.Lines = request.Lines.Select(x => new BillLine
        {
            ProductId = x.ProductId,
            Qty = x.Qty,
            UnitPriceVnd = x.UnitPriceVnd,
            LineTotalVnd = x.UnitPriceVnd * x.Qty,
        }).ToList();

        await dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new ConfirmResponse(
            bill.Id,
            bill.Status,
            bill.PendingAtUtc,
            bill.Subtotal,
            bill.VoucherThuongAmount,
            bill.VoucherType,
            bill.VoucherValue,
            bill.BaseBeforeVip,
            bill.VipPointUsed,
            bill.VipDiscountVnd,
            bill.Payable));
    }

    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, CancellationToken cancellationToken)
    {
        var bill = await dbContext.Bills
            .Include(x => x.Buyer)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (bill is null)
        {
            return NotFound(new ApiError("bill_not_found", "Bill not found."));
        }

        if (bill.Status == BillStatus.Completed)
        {
            return Conflict(new ApiError("bill_already_completed", "Bill is already completed."));
        }

        var earned = bill.Payable / 1_000_000;
        bill.Status = BillStatus.Completed;
        bill.CompletedAtUtc = DateTime.UtcNow;
        bill.VipPointEarned = earned;
        bill.Buyer.VipPoint += (int)earned;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CompleteResponse(
            bill.Id,
            bill.Status,
            bill.CompletedAtUtc,
            bill.Payable,
            bill.VipPointEarned));
    }

    [HttpPost("{id:long}/test-backdate")]
    public async Task<IActionResult> TestBackdate(
        long id,
        [FromBody] TestBackdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var bill = await dbContext.Bills.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (bill is null)
        {
            return NotFound(new ApiError("bill_not_found", "Bill not found."));
        }

        if (bill.Status != BillStatus.Completed)
        {
            return BadRequest(new ApiError("bill_not_completed", "Only Completed bill can be backdated."));
        }

        if (request.PendingAtUtc > request.CompletedAtUtc)
        {
            return BadRequest(new ApiError("invalid_backdate_range", "pendingAtUtc must be <= completedAtUtc."));
        }

        if (request.CompletedAtUtc > DateTime.UtcNow.AddMinutes(1))
        {
            return BadRequest(new ApiError("invalid_completed_time", "completedAtUtc cannot be in the future."));
        }

        bill.PendingAtUtc = request.PendingAtUtc;
        bill.CompletedAtUtc = request.CompletedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new
        {
            billId = bill.Id,
            bill.PendingAtUtc,
            bill.CompletedAtUtc,
            message = "Backdate applied (development only).",
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var bill = await dbContext.Bills
            .AsNoTracking()
            .Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (bill is null)
        {
            return NotFound(new ApiError("bill_not_found", "Bill not found."));
        }

        return Ok(new BillDetailResponse(
            bill.Id,
            bill.BuyerId,
            bill.Status,
            bill.PendingAtUtc,
            bill.CompletedAtUtc,
            bill.Subtotal,
            bill.VoucherThuongAmount,
            bill.VoucherType,
            bill.VoucherValue,
            bill.BaseBeforeVip,
            bill.VipPointUsed,
            bill.VipDiscountVnd,
            bill.Payable,
            bill.VipPointEarned,
            bill.Lines.Select(x => new BillLineDetail(x.ProductId, x.Qty, x.UnitPriceVnd, x.LineTotalVnd)).ToList()));
    }

    private async Task<IActionResult?> ValidateRequest(
        PreviewRequest request,
        CancellationToken cancellationToken,
        int? overrideVipBalance = null)
    {
        if (request.BuyerId <= 0)
        {
            return BadRequest(new ApiError("invalid_buyer_id", "buyerId must be > 0."));
        }

        var buyer = await dbContext.Buyers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.BuyerId, cancellationToken);
        if (buyer is null)
        {
            return NotFound(new ApiError("buyer_not_found", "Buyer not found."));
        }

        if (request.Lines.Count == 0)
        {
            return BadRequest(new ApiError("empty_lines", "At least one draft line is required."));
        }

        if (request.Lines.Any(x => x.Qty <= 0 || x.UnitPriceVnd < 0))
        {
            return BadRequest(new ApiError("invalid_line_values", "Each line must have qty > 0 and unitPriceVnd >= 0."));
        }

        var productIds = request.Lines.Select(x => x.ProductId).Distinct().ToList();
        var validProductIds = await dbContext.Products
            .AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (validProductIds.Count != productIds.Count)
        {
            return BadRequest(new ApiError("invalid_product_id", "One or more productId values are invalid."));
        }

        var voucherType = request.VoucherType?.Trim().ToLowerInvariant();
        if (voucherType is not ("none" or "percent" or "vnd"))
        {
            return BadRequest(new ApiError("invalid_voucher_type", "voucherType must be one of: none, percent, vnd."));
        }

        if (voucherType == "percent" && (request.VoucherValue < 0 || request.VoucherValue > 100))
        {
            return BadRequest(new ApiError("invalid_percent_voucher", "Percent voucher must be in range 0..100."));
        }

        if (voucherType == "vnd" && request.VoucherValue < 0)
        {
            return BadRequest(new ApiError("invalid_vnd_voucher", "Fixed VND voucher must be >= 0."));
        }

        if (request.VipPointsUsed is > 0 and < 5)
        {
            return BadRequest(new ApiError("invalid_vip_points_used", "vipPointsUsed must be 0 or >= 5."));
        }

        var vipBalance = overrideVipBalance ?? buyer.VipPoint;
        if (request.VipPointsUsed > vipBalance)
        {
            return BadRequest(new ApiError("vip_balance_exceeded", "vipPointsUsed exceeds buyer balance."));
        }

        return null;
    }

    private PreviewCalculationResult CalculateFromRequest(PreviewRequest request)
    {
        var voucherType = NormalizeVoucherType(request.VoucherType) switch
        {
            "percent" => VoucherType.Percent,
            "vnd" => VoucherType.FixedVnd,
            _ => VoucherType.None,
        };

        return calculator.Calculate(new PreviewCalculationInput(
            request.Lines.Select(x => new PreviewLineInput(x.UnitPriceVnd, x.Qty)).ToList(),
            voucherType,
            request.VoucherValue,
            request.VipPointsUsed));
    }

    private static string NormalizeVoucherType(string? voucherType)
    {
        return voucherType?.Trim().ToLowerInvariant() switch
        {
            "percent" => "percent",
            "vnd" => "vnd",
            _ => "none",
        };
    }

    private IQueryable<Bill> BuildBillFilterQuery(
        int? buyerId,
        DateTime? from,
        DateTime? to,
        long? payableMaxVnd,
        string? status,
        string? bucket,
        string? timeField,
        List<int>? productIds,
        TimeZoneInfo timeZone)
    {
        var query = dbContext.Bills.AsNoTracking().Include(x => x.Lines).AsQueryable();

        if (buyerId.HasValue)
        {
            query = query.Where(x => x.BuyerId == buyerId.Value);
        }

        var normalizedTimeField = NormalizeTimeField(timeField);
        var fromUtcInclusive = VietnamCivilDayStartUtc(from, timeZone);
        var toUtcExclusive = VietnamCivilDayEndExclusiveUtc(to, timeZone);
        if (from.HasValue)
        {
            query = normalizedTimeField == "completed"
                ? query.Where(x => x.CompletedAtUtc.HasValue && x.CompletedAtUtc.Value >= fromUtcInclusive!.Value)
                : query.Where(x => x.PendingAtUtc >= fromUtcInclusive!.Value);
        }

        if (to.HasValue)
        {
            query = normalizedTimeField == "completed"
                ? query.Where(x => x.CompletedAtUtc.HasValue && x.CompletedAtUtc.Value < toUtcExclusive!.Value)
                : query.Where(x => x.PendingAtUtc < toUtcExclusive!.Value);
        }

        if (payableMaxVnd.HasValue)
        {
            query = query.Where(x => x.Payable <= payableMaxVnd.Value);
        }

        var normalizedStatus = status?.Trim().ToLowerInvariant();
        if (normalizedStatus == "pending")
        {
            query = query.Where(x => x.Status == BillStatus.Pending);
        }
        else if (normalizedStatus == "completed")
        {
            query = query.Where(x => x.Status == BillStatus.Completed);
        }

        if (productIds is { Count: > 0 })
        {
            foreach (var productId in productIds.Distinct())
            {
                var pid = productId;
                query = query.Where(x => x.Lines.Any(l => l.ProductId == pid));
            }
        }

        // Step 4 accepts bucket parameter for future alignment with reports.
        _ = bucket;
        return query;
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }

    private static DateTime VietnamCivilDateOnly(DateTime raw)
    {
        return new DateTime(raw.Year, raw.Month, raw.Day, 0, 0, 0, DateTimeKind.Unspecified);
    }

    private static DateTime? VietnamCivilDayStartUtc(DateTime? value, TimeZoneInfo timeZone)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var civil = VietnamCivilDateOnly(value.Value);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(civil, DateTimeKind.Unspecified), timeZone);
    }

    private static DateTime? VietnamCivilDayEndExclusiveUtc(DateTime? value, TimeZoneInfo timeZone)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var civil = VietnamCivilDateOnly(value.Value).AddDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(civil, DateTimeKind.Unspecified), timeZone);
    }

    private static string NormalizeTimeField(string? timeField)
    {
        return timeField?.Trim().ToLowerInvariant() switch
        {
            "completed" => "completed",
            _ => "pending",
        };
    }

    public sealed record PreviewRequest(
        int BuyerId,
        IReadOnlyList<PreviewLineRequest> Lines,
        string VoucherType = "none",
        decimal VoucherValue = 0,
        int VipPointsUsed = 0);

    public sealed record PreviewLineRequest(int ProductId, int Qty, long UnitPriceVnd);

    public sealed record PreviewResponse(
        long Subtotal,
        long VoucherThuongAmount,
        long BaseBeforeVip,
        long VipDiscount,
        long Payable,
        long ExpectedVipPointsEarnedIfCompleted);

    public sealed record ConfirmResponse(
        long BillId,
        string Status,
        DateTime PendingAtUtc,
        long Subtotal,
        long VoucherThuongAmount,
        string VoucherType,
        decimal VoucherValue,
        long BaseBeforeVip,
        int VipPointUsed,
        long VipDiscountVnd,
        long Payable);

    public sealed record CompleteResponse(
        long BillId,
        string Status,
        DateTime? CompletedAtUtc,
        long Payable,
        long VipPointEarned);

    public sealed record BillDetailResponse(
        long BillId,
        int BuyerId,
        string Status,
        DateTime PendingAtUtc,
        DateTime? CompletedAtUtc,
        long Subtotal,
        long VoucherThuongAmount,
        string VoucherType,
        decimal VoucherValue,
        long BaseBeforeVip,
        int VipPointUsed,
        long VipDiscountVnd,
        long Payable,
        long VipPointEarned,
        IReadOnlyList<BillLineDetail> Lines);

    public sealed record BillLineDetail(int ProductId, int Qty, long UnitPriceVnd, long LineTotalVnd);

    public sealed record BillListItemDto(
        long BillId,
        int BuyerId,
        string Status,
        DateTime PendingAtUtc,
        DateTime? CompletedAtUtc,
        long Subtotal,
        long VoucherThuongAmount,
        string VoucherType,
        decimal VoucherValue,
        long BaseBeforeVip,
        int VipPointUsed,
        long VipDiscountVnd,
        long Payable,
        long VipPointEarned);

    public sealed record BillListResponse(
        IReadOnlyList<BillListItemDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    public sealed record BillCsvRowDto(
        long BillId,
        int BuyerId,
        string Status,
        DateTime PendingAtUtc,
        DateTime? CompletedAtUtc,
        long Subtotal,
        long VoucherThuongAmount,
        string VoucherType,
        decimal VoucherValue,
        long BaseBeforeVip,
        int VipPointUsed,
        long VipDiscountVnd,
        long Payable,
        long VipPointEarned);

    public sealed record TestBackdateRequest(DateTime PendingAtUtc, DateTime CompletedAtUtc);

    private static int NormalizePageSize(int? pageSize)
    {
        return pageSize.GetValueOrDefault(5) switch
        {
            10 => 10,
            15 => 15,
            20 => 20,
            _ => 5,
        };
    }
}
