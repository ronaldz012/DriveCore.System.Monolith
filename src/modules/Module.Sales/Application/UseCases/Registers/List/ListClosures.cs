using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Registers.List;

public class ListClosures(ISalesDbContext context)
{
    public async Task<Result<PagedResultDto<ClosureListDto>>> Execute(ActorContext ctx, ClosuresQueryDto queryDto)
    {
        var query = context.CashRegisterClosures
            .AsNoTracking()
            .Where(c => c.BranchId == ctx.BranchId);

        if (queryDto.DateFrom.HasValue)
            query = query.Where(c => c.OpenAt >= queryDto.DateFrom.Value);

        if (queryDto.DateTo.HasValue)
            query = query.Where(c => c.OpenAt <= queryDto.DateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(c => c.OpenAt)
            .ApplyPagination(queryDto)
            .Select(c => new ClosureListDto
            {
                Id = c.Id,
                Number = c.Number,
                OpenedAt = c.OpenAt,
                ClosedAt = c.ClosedAt,
                OpenedByName = c.OpenByName,
                ClosedByName = c.CloseByName,
                OpeningBalance = c.OpeningBalance,
                TotalSales = c.Sales.Sum(s => s.TotalAmount),
                CashSales = c.Sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.TotalAmount),
                TotalExpenses = c.Movements.Where(m => m.Type == CashRegisterMovementType.Outflow).Sum(m => m.Amount),
                SystemSalesAmount = c.SystemSalesAmount,
                RealCountedAmount = c.RealCountedAmount,
                Difference = c.RealCountedAmount - c.SystemSalesAmount
            })
            .ToListAsync();

        return new PagedResultDto<ClosureListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageSize = queryDto.PageSize,
            Page = queryDto.Page
        };
    }
}
