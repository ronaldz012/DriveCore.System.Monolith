using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Return.List;

public class ListReturns(ISalesDbContext context)
{
    public async Task<Result<PagedResultDto<ReturnListDto>>> Execute(ActorContext ctx, ReturnsQueryDto queryDto)
    {
        var currentBranch = ctx.BranchIds[0];

        var query = context.Sales
            .AsNoTracking()
            .Where(s => s.BranchId == currentBranch && s.Type == SaleType.Return);

        if (queryDto.DateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= queryDto.DateFrom.Value);

        if (queryDto.DateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= queryDto.DateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ApplyPagination(queryDto)
            .Select(s => new ReturnListDto
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
                PaymentMethod = s.PaymentMethod,
                OriginalSaleId = s.OriginalSaleId,
                Notes = s.Notes,
            })
            .ToListAsync();

        return new PagedResultDto<ReturnListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = queryDto.PageSize,
            Page = queryDto.Page
        };
    }
}
