using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Data;
using RevenueModule.Api.Models;

namespace RevenueModule.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(RevenueDbContext dbContext) : ControllerBase
{
    [HttpGet("revenue-series")]
    public async Task<IActionResult> GetRevenueSeries(
        [FromQuery] string mode = "total",
        [FromQuery] int? buyerId = null,
        [FromQuery] int? productId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] long? payableMaxVnd = null,
        [FromQuery] string bucket = "day",
        CancellationToken cancellationToken = default)
    {
        var normalizedMode = mode.Trim().ToLowerInvariant();
        if (normalizedMode is not ("total" or "bybuyer" or "byproduct" or "bybuyerandproduct"))
        {
            return BadRequest(new ApiError("invalid_mode", "mode must be one of: total, byBuyer, byProduct, byBuyerAndProduct."));
        }

        var normalizedBucket = bucket.Trim().ToLowerInvariant();
        if (normalizedBucket is not ("day" or "week" or "month"))
        {
            return BadRequest(new ApiError("invalid_bucket", "bucket must be one of: day, week, month."));
        }

        if (normalizedMode is "bybuyer" or "bybuyerandproduct")
        {
            if (!buyerId.HasValue || buyerId.Value <= 0)
            {
                return BadRequest(new ApiError("buyer_id_required", "buyerId is required for byBuyer and byBuyerAndProduct modes."));
            }
        }

        if (normalizedMode is "byproduct" or "bybuyerandproduct")
        {
            if (!productId.HasValue || productId.Value <= 0)
            {
                return BadRequest(new ApiError("product_id_required", "productId is required for byProduct and byBuyerAndProduct modes."));
            }
        }

        if (from.HasValue && to.HasValue)
        {
            var fromC = VietnamCivilDateOnly(from.Value);
            var toC = VietnamCivilDateOnly(to.Value);
            if (fromC > toC)
            {
                return BadRequest(new ApiError("invalid_date_range", "from must be on or before to."));
            }
        }

        var timeZone = ResolveVietnamTimeZone();
        var query = BuildCompletedBillsQuery(buyerId, productId, from, to, payableMaxVnd, timeZone);
        var bills = await query.ToListAsync(cancellationToken);
        var grouped = new Dictionary<DateTime, long>();

        foreach (var bill in bills)
        {
            var completedUtc = bill.CompletedAtUtc!.Value;
            var completedLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(completedUtc, DateTimeKind.Utc), timeZone);
            var bucketStart = GetBucketStartLocal(completedLocal, normalizedBucket);
            var revenue = normalizedMode switch
            {
                "byproduct" => CalculateAllocatedRevenueForProduct(bill, productId!.Value),
                "bybuyerandproduct" => CalculateAllocatedRevenueForProduct(bill, productId!.Value),
                _ => bill.Payable,
            };
            if (!grouped.TryAdd(bucketStart, revenue))
            {
                grouped[bucketStart] += revenue;
            }
        }

        IReadOnlyList<RevenuePointDto> points;
        if (from.HasValue && to.HasValue)
        {
            var fromC = VietnamCivilDateOnly(from.Value);
            var toC = VietnamCivilDateOnly(to.Value);
            var startBucket = GetBucketStartLocal(fromC, normalizedBucket);
            var endBucket = GetBucketStartLocal(toC, normalizedBucket);
            var keys = EnumerateDenseBucketRange(startBucket, endBucket, normalizedBucket);
            points = keys.Select(k => new RevenuePointDto(k, grouped.GetValueOrDefault(k, 0))).ToList();
        }
        else
        {
            points = grouped
                .OrderBy(x => x.Key)
                .Select(x => new RevenuePointDto(x.Key, x.Value))
                .ToList();
        }

        return Ok(new RevenueSeriesResponse(normalizedMode, normalizedBucket, points));
    }

    [HttpGet("export-nontech.xlsx")]
    public async Task<IActionResult> ExportNonTechXlsx(
        [FromQuery] int? buyerId = null,
        [FromQuery] int? productId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] long? payableMaxVnd = null,
        [FromQuery] string bucket = "day",
        CancellationToken cancellationToken = default)
    {
        var normalizedBucket = bucket.Trim().ToLowerInvariant();
        if (normalizedBucket is not ("day" or "week" or "month"))
        {
            return BadRequest(new ApiError("invalid_bucket", "bucket must be one of: day, week, month."));
        }

        var timeZone = ResolveVietnamTimeZone();

        var completedBills = await BuildCompletedBillsQuery(buyerId, productId, from, to, payableMaxVnd, timeZone)
            .ToListAsync(cancellationToken);

        var pendingCount = await BuildPendingCountQuery(buyerId, productId, from, to, timeZone).CountAsync(cancellationToken);

        var buyerIdsNeeded = completedBills.Select(b => b.BuyerId).Distinct().ToList();
        var buyersMap = await dbContext.Buyers
            .AsNoTracking()
            .Where(x => buyerIdsNeeded.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var productIdsNeeded = completedBills
            .SelectMany(b => b.Lines)
            .Select(l => l.ProductId)
            .Distinct()
            .ToList();
        var productsMap = await dbContext.Products
            .AsNoTracking()
            .Where(x => productIdsNeeded.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        using var workbook = new XLWorkbook();
        BuildSummarySheet(workbook, completedBills, pendingCount, buyerId, productId, from, to, payableMaxVnd, normalizedBucket, buyersMap, productsMap);
        BuildTimeSeriesSheet(workbook, completedBills, normalizedBucket, timeZone, from, to);
        BuildByBuyerSheet(workbook, completedBills, buyersMap);
        BuildByProductSheet(workbook, completedBills, productsMap, productId);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "revenue-nontech.xlsx");
    }

    private IQueryable<Bill> BuildCompletedBillsQuery(
        int? buyerId,
        int? productId,
        DateTime? from,
        DateTime? to,
        long? payableMaxVnd,
        TimeZoneInfo timeZone)
    {
        var query = dbContext.Bills
            .AsNoTracking()
            .Where(x => x.Status == BillStatus.Completed && x.CompletedAtUtc != null)
            .Include(x => x.Lines)
            .AsQueryable();

        var fromUtcInclusive = VietnamCivilDayStartUtc(from, timeZone);
        var toUtcExclusive = VietnamCivilDayEndExclusiveUtc(to, timeZone);

        if (fromUtcInclusive.HasValue)
        {
            query = query.Where(x => x.CompletedAtUtc >= fromUtcInclusive.Value);
        }

        if (toUtcExclusive.HasValue)
        {
            query = query.Where(x => x.CompletedAtUtc < toUtcExclusive.Value);
        }

        if (buyerId.HasValue)
        {
            query = query.Where(x => x.BuyerId == buyerId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.Lines.Any(l => l.ProductId == productId.Value));
        }

        if (payableMaxVnd.HasValue)
        {
            query = query.Where(x => x.Payable <= payableMaxVnd.Value);
        }

        return query;
    }

    private IQueryable<Bill> BuildPendingCountQuery(
        int? buyerId,
        int? productId,
        DateTime? from,
        DateTime? to,
        TimeZoneInfo timeZone)
    {
        var query = dbContext.Bills
            .AsNoTracking()
            .Where(x => x.Status == BillStatus.Pending)
            .AsQueryable();

        var fromUtcInclusive = VietnamCivilDayStartUtc(from, timeZone);
        var toUtcExclusive = VietnamCivilDayEndExclusiveUtc(to, timeZone);

        if (fromUtcInclusive.HasValue)
        {
            query = query.Where(x => x.PendingAtUtc >= fromUtcInclusive.Value);
        }

        if (toUtcExclusive.HasValue)
        {
            query = query.Where(x => x.PendingAtUtc < toUtcExclusive.Value);
        }

        if (buyerId.HasValue)
        {
            query = query.Where(x => x.BuyerId == buyerId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.Lines.Any(l => l.ProductId == productId.Value));
        }

        return query;
    }

    private static void BuildSummarySheet(
        XLWorkbook workbook,
        List<Bill> completedBills,
        int pendingCount,
        int? buyerId,
        int? productId,
        DateTime? from,
        DateTime? to,
        long? payableMaxVnd,
        string bucket,
        IReadOnlyDictionary<int, string> buyersMap,
        IReadOnlyDictionary<int, string> productsMap)
    {
        var ws = workbook.Worksheets.Add("Summary");

        var totalRevenue = completedBills.Sum(b => b.Payable);
        var completedCount = completedBills.Count;
        var aov = completedCount > 0 ? totalRevenue / completedCount : 0;
        var totalVoucher = completedBills.Sum(b => b.VoucherThuongAmount);
        var totalVipDiscount = completedBills.Sum(b => b.VipDiscountVnd);
        var totalVipUsed = completedBills.Sum(b => (long)b.VipPointUsed);
        var totalVipEarned = completedBills.Sum(b => b.VipPointEarned);
        var pipelineTotal = completedCount + pendingCount;
        var completionRate = pipelineTotal > 0 ? (double)completedCount / pipelineTotal : 0;

        var buyerLabel = buyerId.HasValue
            ? $"#{buyerId.Value} - {(buyersMap.TryGetValue(buyerId.Value, out var n) ? n : "(unknown)")}"
            : "Tất cả";
        var productLabel = productId.HasValue
            ? $"#{productId.Value} - {(productsMap.TryGetValue(productId.Value, out var pn) ? pn : "(unknown)")}"
            : "Tất cả";
        var payableMaxLabel = payableMaxVnd.HasValue ? payableMaxVnd.Value.ToString("N0") + " VND" : "Không giới hạn";

        var rows = new (string Label, object Value, string? Format)[]
        {
            ("Báo cáo doanh thu (non-tech)", string.Empty, null),
            ("Mốc thời gian áp dụng", "CompletedAt (chỉ tính bill đã Complete)", null),
            ("Khoảng từ (UTC)", from.HasValue ? from.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Không giới hạn", null),
            ("Khoảng đến (UTC)", to.HasValue ? to.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Không giới hạn", null),
            ("Bucket thời gian", bucket, null),
            ("Buyer filter", buyerLabel, null),
            ("Product filter", productLabel, null),
            ("Payable tối đa", payableMaxLabel, null),
            (string.Empty, string.Empty, null),
            ("Doanh thu (Completed)", totalRevenue, "#,##0\" VND\""),
            ("Số bill Completed", completedCount, "#,##0"),
            ("Số bill Pending (cùng kỳ, theo PendingAt)", pendingCount, "#,##0"),
            ("AOV (doanh thu / bill)", aov, "#,##0\" VND\""),
            ("Tổng giảm voucher thường", totalVoucher, "#,##0\" VND\""),
            ("Tổng giảm VIP", totalVipDiscount, "#,##0\" VND\""),
            ("Tổng VIP Point dùng", totalVipUsed, "#,##0"),
            ("Tổng VIP Point nhận", totalVipEarned, "#,##0"),
            ("Tỷ lệ hoàn tất", completionRate, "0.00%"),
        };

        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i];
            var rowIndex = i + 1;
            var labelCell = ws.Cell(rowIndex, 1);
            labelCell.Value = row.Label;
            if (i == 0)
            {
                labelCell.Style.Font.Bold = true;
                labelCell.Style.Font.FontSize = 14;
                ws.Range(rowIndex, 1, rowIndex, 2).Merge();
                continue;
            }

            var valueCell = ws.Cell(rowIndex, 2);
            switch (row.Value)
            {
                case long longVal:
                    valueCell.Value = longVal;
                    break;
                case int intVal:
                    valueCell.Value = intVal;
                    break;
                case double doubleVal:
                    valueCell.Value = doubleVal;
                    break;
                default:
                    valueCell.Value = row.Value?.ToString() ?? string.Empty;
                    break;
            }
            if (!string.IsNullOrEmpty(row.Format))
            {
                valueCell.Style.NumberFormat.Format = row.Format;
            }
            labelCell.Style.Font.Bold = true;
        }

        ws.Columns().AdjustToContents();
        ws.Column(1).Width = Math.Max(ws.Column(1).Width, 38);
        ws.Column(2).Width = Math.Max(ws.Column(2).Width, 28);
    }

    private static void BuildTimeSeriesSheet(
        XLWorkbook workbook,
        List<Bill> completedBills,
        string bucket,
        TimeZoneInfo timeZone,
        DateTime? rangeFrom = null,
        DateTime? rangeTo = null)
    {
        var ws = workbook.Worksheets.Add("TimeSeries");
        ws.Cell(1, 1).Value = "Bucket (giờ địa phương)";
        ws.Cell(1, 2).Value = "Doanh thu (VND)";
        ws.Cell(1, 3).Value = "Số bill Completed";
        ws.Cell(1, 4).Value = "AOV (VND)";
        ws.Range(1, 1, 1, 4).Style.Font.Bold = true;

        var grouped = new Dictionary<DateTime, (long Revenue, int Count)>();
        foreach (var bill in completedBills)
        {
            var completedLocal = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(bill.CompletedAtUtc!.Value, DateTimeKind.Utc),
                timeZone);
            var bucketStart = GetBucketStartLocal(completedLocal, bucket);
            if (grouped.TryGetValue(bucketStart, out var current))
            {
                grouped[bucketStart] = (current.Revenue + bill.Payable, current.Count + 1);
            }
            else
            {
                grouped[bucketStart] = (bill.Payable, 1);
            }
        }

        List<(DateTime Key, long Revenue, int Count)> ordered;
        if (rangeFrom.HasValue && rangeTo.HasValue)
        {
            var fromC = VietnamCivilDateOnly(rangeFrom.Value);
            var toC = VietnamCivilDateOnly(rangeTo.Value);
            if (fromC <= toC)
            {
                var startBucket = GetBucketStartLocal(fromC, bucket);
                var endBucket = GetBucketStartLocal(toC, bucket);
                var keys = EnumerateDenseBucketRange(startBucket, endBucket, bucket);
                ordered = keys.Select(k =>
                {
                    if (!grouped.TryGetValue(k, out var row))
                    {
                        row = (0L, 0);
                    }

                    return (k, row.Revenue, row.Count);
                }).ToList();
            }
            else
            {
                ordered = grouped
                    .OrderBy(kv => kv.Key)
                    .Select(kv => (kv.Key, kv.Value.Revenue, kv.Value.Count))
                    .ToList();
            }
        }
        else
        {
            ordered = grouped
                .OrderBy(kv => kv.Key)
                .Select(kv => (kv.Key, kv.Value.Revenue, kv.Value.Count))
                .ToList();
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            var rowIndex = i + 2;
            var entry = ordered[i];
            ws.Cell(rowIndex, 1).Value = FormatBucketLabel(entry.Key, bucket);
            ws.Cell(rowIndex, 2).Value = entry.Revenue;
            ws.Cell(rowIndex, 2).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 3).Value = entry.Count;
            ws.Cell(rowIndex, 3).Style.NumberFormat.Format = "#,##0";
            var aov = entry.Count > 0 ? entry.Revenue / entry.Count : 0;
            ws.Cell(rowIndex, 4).Value = aov;
            ws.Cell(rowIndex, 4).Style.NumberFormat.Format = "#,##0";
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildByBuyerSheet(
        XLWorkbook workbook,
        List<Bill> completedBills,
        IReadOnlyDictionary<int, string> buyersMap)
    {
        var ws = workbook.Worksheets.Add("ByBuyer");
        ws.Cell(1, 1).Value = "Buyer ID";
        ws.Cell(1, 2).Value = "Buyer";
        ws.Cell(1, 3).Value = "Doanh thu (VND)";
        ws.Cell(1, 4).Value = "Số bill";
        ws.Cell(1, 5).Value = "AOV (VND)";
        ws.Cell(1, 6).Value = "VIP Point dùng";
        ws.Cell(1, 7).Value = "VIP Point nhận";
        ws.Range(1, 1, 1, 7).Style.Font.Bold = true;

        var grouped = completedBills
            .GroupBy(b => b.BuyerId)
            .Select(g => new
            {
                BuyerId = g.Key,
                Revenue = g.Sum(x => x.Payable),
                Count = g.Count(),
                VipUsed = g.Sum(x => (long)x.VipPointUsed),
                VipEarned = g.Sum(x => x.VipPointEarned),
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        for (var i = 0; i < grouped.Count; i++)
        {
            var rowIndex = i + 2;
            var row = grouped[i];
            ws.Cell(rowIndex, 1).Value = row.BuyerId;
            ws.Cell(rowIndex, 2).Value = buyersMap.TryGetValue(row.BuyerId, out var name) ? name : "(unknown)";
            ws.Cell(rowIndex, 3).Value = row.Revenue;
            ws.Cell(rowIndex, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 4).Value = row.Count;
            ws.Cell(rowIndex, 4).Style.NumberFormat.Format = "#,##0";
            var aov = row.Count > 0 ? row.Revenue / row.Count : 0;
            ws.Cell(rowIndex, 5).Value = aov;
            ws.Cell(rowIndex, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 6).Value = row.VipUsed;
            ws.Cell(rowIndex, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 7).Value = row.VipEarned;
            ws.Cell(rowIndex, 7).Style.NumberFormat.Format = "#,##0";
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildByProductSheet(
        XLWorkbook workbook,
        List<Bill> completedBills,
        IReadOnlyDictionary<int, string> productsMap,
        int? productFilter)
    {
        var ws = workbook.Worksheets.Add("ByProduct");
        ws.Cell(1, 1).Value = "Product ID";
        ws.Cell(1, 2).Value = "Product";
        ws.Cell(1, 3).Value = "Doanh thu phân bổ (VND)";
        ws.Cell(1, 4).Value = "Số lượng bán";
        ws.Cell(1, 5).Value = "Số bill chứa product";
        ws.Range(1, 1, 1, 5).Style.Font.Bold = true;

        var allocations = new Dictionary<int, (long Revenue, long Qty, HashSet<long> BillIds)>();
        foreach (var bill in completedBills)
        {
            var perBill = AllocateRevenuePerLine(bill);
            for (var i = 0; i < bill.Lines.Count; i++)
            {
                var line = bill.Lines[i];
                if (productFilter.HasValue && line.ProductId != productFilter.Value)
                {
                    continue;
                }

                var allocated = perBill[i];
                if (allocations.TryGetValue(line.ProductId, out var current))
                {
                    current.BillIds.Add(bill.Id);
                    allocations[line.ProductId] = (current.Revenue + allocated, current.Qty + line.Qty, current.BillIds);
                }
                else
                {
                    allocations[line.ProductId] = (allocated, line.Qty, new HashSet<long> { bill.Id });
                }
            }
        }

        var ordered = allocations
            .Select(kv => new { ProductId = kv.Key, kv.Value.Revenue, kv.Value.Qty, BillCount = kv.Value.BillIds.Count })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            var rowIndex = i + 2;
            var row = ordered[i];
            ws.Cell(rowIndex, 1).Value = row.ProductId;
            ws.Cell(rowIndex, 2).Value = productsMap.TryGetValue(row.ProductId, out var name) ? name : "(unknown)";
            ws.Cell(rowIndex, 3).Value = row.Revenue;
            ws.Cell(rowIndex, 3).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 4).Value = row.Qty;
            ws.Cell(rowIndex, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 5).Value = row.BillCount;
            ws.Cell(rowIndex, 5).Style.NumberFormat.Format = "#,##0";
        }

        ws.Columns().AdjustToContents();
    }

    private static long[] AllocateRevenuePerLine(Bill bill)
    {
        var lineCount = bill.Lines.Count;
        var allocated = new long[lineCount];
        if (lineCount == 0 || bill.Subtotal <= 0 || bill.Payable <= 0)
        {
            return allocated;
        }

        var rows = bill.Lines
            .Select((line, index) =>
            {
                var raw = line.LineTotalVnd * bill.Payable;
                var baseAlloc = raw / bill.Subtotal;
                var remainder = raw % bill.Subtotal;
                return new AllocationRow(index, line.ProductId, baseAlloc, remainder);
            })
            .ToList();

        var diff = bill.Payable - rows.Sum(x => x.BaseAllocated);
        foreach (var row in rows
            .OrderByDescending(x => x.Remainder)
            .ThenBy(x => x.Index)
            .Take((int)diff))
        {
            row.BaseAllocated += 1;
        }

        foreach (var row in rows)
        {
            allocated[row.Index] = row.BaseAllocated;
        }

        return allocated;
    }

    private static string FormatBucketLabel(DateTime bucketStart, string bucket)
    {
        return bucket switch
        {
            "month" => bucketStart.ToString("yyyy-MM"),
            "week" => "Tuần " + bucketStart.ToString("yyyy-MM-dd"),
            _ => bucketStart.ToString("yyyy-MM-dd"),
        };
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

    private static DateTime GetBucketStartLocal(DateTime localDateTime, string bucket)
    {
        var date = localDateTime.Date;
        if (bucket == "day")
        {
            return date;
        }

        if (bucket == "week")
        {
            var diff = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-diff);
        }

        return new DateTime(date.Year, date.Month, 1);
    }

    private static DateTime VietnamCivilDateOnly(DateTime raw)
    {
        return new DateTime(raw.Year, raw.Month, raw.Day, 0, 0, 0, DateTimeKind.Unspecified);
    }

    private static DateTime? VietnamCivilDayStartUtc(DateTime? from, TimeZoneInfo tz)
    {
        if (!from.HasValue)
        {
            return null;
        }

        var civil = VietnamCivilDateOnly(from.Value);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(civil, DateTimeKind.Unspecified), tz);
    }

    private static DateTime? VietnamCivilDayEndExclusiveUtc(DateTime? to, TimeZoneInfo tz)
    {
        if (!to.HasValue)
        {
            return null;
        }

        var civil = VietnamCivilDateOnly(to.Value);
        var startOfNextLocalDay = civil.AddDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startOfNextLocalDay, DateTimeKind.Unspecified), tz);
    }

    private static DateTime StepBucketStart(DateTime current, string bucket) => bucket switch
    {
        "day" => current.AddDays(1),
        "week" => current.AddDays(7),
        "month" => current.AddMonths(1),
        _ => current,
    };

    private static List<DateTime> EnumerateDenseBucketRange(DateTime startBucket, DateTime endBucket, string bucket)
    {
        var keys = new List<DateTime>();
        if (endBucket < startBucket)
        {
            return keys;
        }

        for (var cur = startBucket; cur <= endBucket; cur = StepBucketStart(cur, bucket))
        {
            keys.Add(cur);
        }

        return keys;
    }

    private static long CalculateAllocatedRevenueForProduct(Bill bill, int productId)
    {
        if (bill.Subtotal <= 0 || bill.Payable <= 0)
        {
            return 0;
        }

        var lineAllocations = bill.Lines
            .Select((line, index) =>
            {
                var raw = line.LineTotalVnd * bill.Payable;
                var baseAlloc = raw / bill.Subtotal;
                var remainder = raw % bill.Subtotal;
                return new AllocationRow(index, line.ProductId, baseAlloc, remainder);
            })
            .ToList();

        var diff = bill.Payable - lineAllocations.Sum(x => x.BaseAllocated);
        foreach (var row in lineAllocations
            .OrderByDescending(x => x.Remainder)
            .ThenBy(x => x.Index)
            .Take((int)diff))
        {
            row.BaseAllocated += 1;
        }

        return lineAllocations
            .Where(x => x.ProductId == productId)
            .Sum(x => x.BaseAllocated);
    }

    private sealed record AllocationRow(int Index, int ProductId, long InitialAllocation, long Remainder)
    {
        public long BaseAllocated { get; set; } = InitialAllocation;
    }

    public sealed record RevenueSeriesResponse(
        string Mode,
        string Bucket,
        IReadOnlyList<RevenuePointDto> Points);

    public sealed record RevenuePointDto(DateTime BucketStartLocal, long RevenueVnd);
}
