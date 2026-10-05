using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;

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

        if (queryDto.DateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= queryDto.DateFrom.Value);

        if (queryDto.DateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= queryDto.DateTo.Value);

        if (queryDto.HasReturn.HasValue)
            query = query.Where(s => s.Returns.Any() == queryDto.HasReturn.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ApplyPagination(queryDto)
            .Select(s => new SaleListDto
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
                PaymentMethod = s.PaymentMethod,
                DocumentType = s.DocumentType,
                InvoiceNumber = s.InvoiceNumber,
                TransactionCode = s.TransactionCode,
                HasReturn = s.Returns.Any(),
                ReturnedAmount = s.Returns.Sum(r => r.TotalAmount),
            })
            .ToListAsync();

        return new PagedResultDto<SaleListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = queryDto.PageSize,
            Page = queryDto.Page
        };
    }
}