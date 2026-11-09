using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Get;

public class GetListSales(ISalesDbContext context)
{
    public async Task<Result<PagedResultDto<SaleListDto>>> Execute(ActorContext ctx, SalesQueryDto queryDto)
    {
        var currentBranch = ctx.BranchIds[0];

        var query = context.Sales
            .AsNoTracking()
            .Where(s => s.BranchId == currentBranch);

        if (queryDto.Type.HasValue)
            query = query.Where(s => s.Type == queryDto.Type.Value);

        if (queryDto.SoldById.HasValue)
            query = query.Where(s => s.SoldById == queryDto.SoldById.Value);

        if (queryDto.DateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= queryDto.DateFrom.Value);

        if (queryDto.DateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= queryDto.DateTo.Value);

        if (queryDto.ReturnStatus.HasValue)
            query = ApplyReturnStatusFilter(query, queryDto.ReturnStatus.Value);
        else if (queryDto.HasReturn.HasValue)
            // Legacy flag: true = any return (Partial or Full).
            query = query.Where(s => s.Returns.Any() == queryDto.HasReturn.Value);

        var filter = queryDto.Filter?.Trim();

        if (!string.IsNullOrEmpty(filter))
        {
            var isNumber = filter.Length <= 9 && filter.All(char.IsAsciiDigit);

            if (isNumber && int.TryParse(filter, out var number))
            {
                // Exact sale number or exact SKU first; fall back to text search so
                // numeric product names (e.g. "32193") stay reachable.
                var exactMatch = query.Where(s =>
                    s.Number == number ||
                    s.SaleItems.Any(si => si.ProductSku == filter));

                query = await exactMatch.AnyAsync()
                    ? exactMatch
                    : ApplyTextFilter(query, filter);
            }
            else
            {
                query = ApplyTextFilter(query, filter);
            }
        }

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderByDescending(s => s.CreatedAt)
            .ApplyPagination(queryDto)
            .Select(s => new
            {
                Sale = new SaleListDto
                {
                    Id = s.Id,
                    Number = s.Number,
                    CreatedAt = s.CreatedAt,
                    TotalAmount = s.TotalAmount,
                    SoldByName = s.SoldByName,
                    FirstItemDisplayName = s.SaleItems
                        .OrderBy(si => si.Id)
                        .Select(si => si.ProductDisplayName)
                        .FirstOrDefault() ?? "",
                    TotalQuantity = s.SaleItems.Sum(si => Math.Abs(si.Quantity)),
                    TotalDistinctItems = s.SaleItems.Count,
                    Type = s.Type,
                    OriginalSaleId = s.OriginalSaleId,
                    OriginalSaleNumber = s.OriginalSale != null ? s.OriginalSale.Number : (int?)null,
                    PaymentMethod = s.PaymentMethod,
                    DocumentType = s.DocumentType,
                    InvoiceNumber = s.InvoiceNumber,
                    TransactionCode = s.TransactionCode,
                    HasReturn = s.Returns.Any(),
                    // Return documents store negative totals; expose the refunded metric as positive.
                    ReturnedAmount = Math.Abs(s.Returns.Sum(r => r.TotalAmount)),
                },
                ReturnedQuantity = s.SaleItems
                    .SelectMany(si => si.ChildReturns)
                    .Sum(cr => -cr.Quantity)
            })
            .ToListAsync();

        var items = rows.Select(row =>
        {
            // Full = every sold unit came back; anything lower is a partial return.
            row.Sale.ReturnStatus = !row.Sale.HasReturn
                ? SaleReturnStatus.None
                : row.ReturnedQuantity >= row.Sale.TotalQuantity
                    ? SaleReturnStatus.Full
                    : SaleReturnStatus.Partial;

            return row.Sale;
        }).ToList();

        return new PagedResultDto<SaleListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = queryDto.PageSize,
            Page = queryDto.Page
        };
    }

    private static IQueryable<Sale> ApplyTextFilter(IQueryable<Sale> query, string filter)
    {
        var words = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var pattern = $"%{word}%";

            // Word must appear in any item's display name or SKU.
            query = query.Where(s => s.SaleItems.Any(si =>
                EF.Functions.ILike(si.ProductDisplayName, pattern) ||
                EF.Functions.ILike(si.ProductSku, pattern)));
        }

        return query;
    }

    private static IQueryable<Sale> ApplyReturnStatusFilter(IQueryable<Sale> query, SaleReturnStatus status) => status switch
    {
        SaleReturnStatus.None => query.Where(s => !s.Returns.Any()),
        SaleReturnStatus.Partial => query.Where(s =>
            s.Returns.Any() &&
            s.SaleItems.SelectMany(si => si.ChildReturns).Sum(cr => -cr.Quantity) <
            s.SaleItems.Sum(si => si.Quantity)),
        SaleReturnStatus.Full => query.Where(s =>
            s.Returns.Any() &&
            s.SaleItems.SelectMany(si => si.ChildReturns).Sum(cr => -cr.Quantity) >=
            s.SaleItems.Sum(si => si.Quantity)),
        _ => query
    };
}
