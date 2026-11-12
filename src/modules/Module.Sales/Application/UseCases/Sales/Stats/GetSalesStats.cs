using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Stats;

public class GetSalesStats(ISalesDbContext context)
{
    private const int TopProductsCount = 10;

    public async Task<Result<SalesStatsDto>> Execute(
        ActorContext ctx,
        SalesStatsQueryDto query,
        bool includeMargin,
        bool includeSellerBreakdown)
    {
        if (query.DateFrom is null || query.DateTo is null)
            return GetSalesStatsErrors.DateRangeRequired;

        if (query.DateTo.Value < query.DateFrom.Value)
            return GetSalesStatsErrors.InvalidDateRange;

        if (query.TzOffsetMinutes is < -840 or > 840)
            return GetSalesStatsErrors.InvalidTimeZoneOffset;

        var from = query.DateFrom.Value;

        // A date-only upper bound means "include the whole day".
        var to = query.DateTo.Value.TimeOfDay == TimeSpan.Zero
            ? query.DateTo.Value.Date.AddDays(1)
            : query.DateTo.Value;

        if (to <= from)
            return GetSalesStatsErrors.InvalidDateRange;

        var scoped = context.Sales
            .AsNoTracking()
            .Where(s => s.BranchId == ctx.BranchId && s.CreatedAt >= from && s.CreatedAt < to);

        if (query.SoldById.HasValue)
        {
            var sellerId = query.SoldById.Value;

            // Sales belong to their own seller; returns belong to the seller of the original sale
            // (e.g. an exchange processed by another cashier must not hit the wrong seller).
            scoped = scoped.Where(s =>
                (s.Type == SaleType.Sale && s.SoldById == sellerId) ||
                (s.Type == SaleType.Return && s.OriginalSale != null && s.OriginalSale.SoldById == sellerId));
        }

        // ---------------------------------------------------------------
        // Totals: sale/return headers
        // ---------------------------------------------------------------
        var headers = await scoped
            .GroupBy(s => s.Type)
            .Select(g => new
            {
                Type = g.Key,
                Count = g.Count(),
                Amount = g.Sum(s => s.TotalAmount)
            })
            .ToListAsync();

        // ---------------------------------------------------------------
        // Totals: item level (units, discounts, free items, margin)
        // ---------------------------------------------------------------
        var itemTotals = await scoped
            .SelectMany(s => s.SaleItems.Select(si => new { s.Type, Item = si }))
            .GroupBy(x => x.Type)
            .Select(g => new
            {
                Type = g.Key,
                Units = g.Sum(x => x.Item.Quantity),
                Discount = g.Sum(x => x.Item.DiscountAmount * x.Item.Quantity),
                FreeAmount = g.Sum(x => x.Item.ProductVariantId == null ? x.Item.FinalPrice : 0m),
                Margin = g.Sum(x => x.Item.FinalPrice - (x.Item.UnitCost ?? 0m) * x.Item.Quantity),
                WithCostAmount = g.Sum(x => x.Item.UnitCost != null ? x.Item.FinalPrice : 0m)
            })
            .ToListAsync();

        var freeSalesCount = await scoped.CountAsync(s =>
            s.Type == SaleType.Sale && s.SaleItems.Any(si => si.ProductVariantId == null));

        // ---------------------------------------------------------------
        // Breakdowns
        // ---------------------------------------------------------------
        var byDay = await scoped
            .GroupBy(s => s.CreatedAt.AddMinutes(query.TzOffsetMinutes).Date)
            .Select(g => new
            {
                Date = g.Key,
                SalesCount = g.Count(s => s.Type == SaleType.Sale),
                Net = g.Sum(s => s.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var byPayment = await scoped
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new
            {
                Method = g.Key,
                SalesCount = g.Count(s => s.Type == SaleType.Sale),
                Amount = g.Sum(s => s.TotalAmount)
            })
            .ToListAsync();

        List<SalesStatsBySellerDto> bySeller = [];

        if (includeSellerBreakdown)
        {
            var sellerTotals = await scoped
                .Select(s => new
                {
                    // Returns are attributed to the seller of the original sale.
                    SellerId = s.Type == SaleType.Sale
                        ? s.SoldById
                        : s.OriginalSale != null ? s.OriginalSale.SoldById : s.SoldById,
                    s.Type,
                    s.TotalAmount
                })
                .GroupBy(x => x.SellerId)
                .Select(g => new
                {
                    Id = g.Key,
                    SalesCount = g.Count(x => x.Type == SaleType.Sale),
                    Net = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var sellerIds = sellerTotals.Select(x => x.Id).ToList();

            var sellerNames = await context.Sales
                .AsNoTracking()
                .Where(s => s.BranchId == ctx.BranchId && sellerIds.Contains(s.SoldById))
                .GroupBy(s => s.SoldById)
                .Select(g => new
                {
                    Id = g.Key,
                    // Seller name is a snapshot per sale; use the most recent one.
                    Name = g.OrderByDescending(s => s.CreatedAt).Select(s => s.SoldByName).First()
                })
                .ToListAsync();

            bySeller = sellerTotals
                .Select(x => new SalesStatsBySellerDto
                {
                    Id = x.Id,
                    Name = sellerNames.FirstOrDefault(n => n.Id == x.Id)?.Name ?? string.Empty,
                    SalesCount = x.SalesCount,
                    NetSalesAmount = Round(x.Net),
                    AvgTicket = x.SalesCount == 0 ? 0 : Round(x.Net / x.SalesCount)
                })
                .OrderByDescending(x => x.NetSalesAmount)
                .ToList();
        }

        // ---------------------------------------------------------------
        // Top products: registered variants only
        // (free items have no reliable identity to group by)
        // ---------------------------------------------------------------
        var topRegistered = await scoped
            .SelectMany(s => s.SaleItems)
            .Where(si => si.ProductVariantId != null)
            .GroupBy(si => si.ProductVariantId!.Value)
            .Select(g => new
            {
                ProductId = g.Key,
                Name = g.Max(si => si.ProductDisplayName),
                Sku = g.Max(si => si.ProductSku),
                Units = g.Sum(si => si.Quantity),
                Amount = g.Sum(si => si.FinalPrice),
                Margin = g.Sum(si => si.FinalPrice - (si.UnitCost ?? 0m) * si.Quantity),
                HasCost = g.Any(si => si.UnitCost != null)
            })
            .OrderByDescending(x => x.Amount)
            .Take(TopProductsCount)
            .ToListAsync();

        var topProducts = topRegistered
            .Select(x => new SalesStatsTopProductDto
            {
                ProductVariantId = x.ProductId,
                ProductName = x.Name ?? string.Empty,
                ProductSku = x.Sku ?? string.Empty,
                Units = x.Units,
                NetSalesAmount = Round(x.Amount),
                GrossMargin = includeMargin && x.HasCost ? Round(x.Margin) : null
            })
            .ToList();

        // ---------------------------------------------------------------
        // Compose
        // ---------------------------------------------------------------
        var sales = headers.FirstOrDefault(h => h.Type == SaleType.Sale);
        var returns = headers.FirstOrDefault(h => h.Type == SaleType.Return);
        var salesItems = itemTotals.FirstOrDefault(x => x.Type == SaleType.Sale);
        var returnItems = itemTotals.FirstOrDefault(x => x.Type == SaleType.Return);

        var salesCount = sales?.Count ?? 0;
        var grossSales = sales?.Amount ?? 0m;
        var returnsAmount = Math.Abs(returns?.Amount ?? 0m);
        var netSales = grossSales - returnsAmount;

        var marginNet = (salesItems?.Margin ?? 0m) + (returnItems?.Margin ?? 0m);
        var withCostNet = (salesItems?.WithCostAmount ?? 0m) + (returnItems?.WithCostAmount ?? 0m);

        var totals = new SalesStatsTotalsDto
        {
            SalesCount = salesCount,
            ReturnsCount = returns?.Count ?? 0,
            GrossSalesAmount = Round(grossSales),
            ReturnsAmount = Round(returnsAmount),
            NetSalesAmount = Round(netSales),
            UnitsSold = (int)Math.Abs(salesItems?.Units ?? 0),
            UnitsReturned = (int)Math.Abs(returnItems?.Units ?? 0),
            AvgTicket = salesCount == 0 ? 0 : Round(netSales / salesCount),
            DiscountAmount = Round(Math.Abs(salesItems?.Discount ?? 0m)),
            FreeSalesAmount = Round(salesItems?.FreeAmount ?? 0m),
            FreeSalesCount = freeSalesCount,
            GrossMargin = includeMargin ? Round(marginNet) : null,
            MarginPct = includeMargin ? Pct(marginNet, netSales) : null,
            MarginCoveragePct = includeMargin ? Pct(withCostNet, netSales) : null
        };

        return new SalesStatsDto
        {
            Totals = totals,
            ByDay = byDay
                .Select(x => new SalesStatsByDayDto
                {
                    Date = DateOnly.FromDateTime(x.Date),
                    SalesCount = x.SalesCount,
                    NetSalesAmount = Round(x.Net)
                })
                .ToList(),
            ByPaymentMethod = byPayment
                .Select(x => new SalesStatsByPaymentMethodDto
                {
                    Method = x.Method,
                    SalesCount = x.SalesCount,
                    Amount = Round(x.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .ToList(),
            BySeller = bySeller,
            TopProducts = topProducts
        };
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static decimal Pct(decimal numerator, decimal denominator)
        => denominator == 0 ? 0 : Round(numerator / denominator * 100);
}

public static class GetSalesStatsErrors
{
    public static readonly Error DateRangeRequired =
        new(ErrorCode.ValidationError, "dateFrom and dateTo are required.");

    public static readonly Error InvalidDateRange =
        new(ErrorCode.ValidationError, "dateTo must be greater than dateFrom.");

    public static readonly Error InvalidTimeZoneOffset =
        new(ErrorCode.ValidationError, "tzOffsetMinutes must be between -840 and 840.");
}
