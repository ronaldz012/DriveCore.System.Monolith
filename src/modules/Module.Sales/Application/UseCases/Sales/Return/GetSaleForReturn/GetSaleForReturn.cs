using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Return.GetSaleForReturn;

public class GetSaleForReturn(ISalesDbContext context)
{
    public async Task<Result<SaleForReturnDto>> Execute(ActorContext ctx, Guid saleId)
    {
        var currentBranch = ctx.BranchIds[0];

        var sale = await context.Sales
            .AsNoTracking()
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == saleId && s.BranchId == currentBranch);

        if (sale == null)
            return ReturnErrors.OriginalSaleNotFound;

        if (sale.Type != SaleType.Sale)
            return ReturnErrors.OriginalSaleNotEligible;

        var hasReturn = await context.Sales
            .AnyAsync(s => s.OriginalSaleId == saleId && s.Type == SaleType.Return);
        if (hasReturn)
            return ReturnErrors.AlreadyReturned;

        return new SaleForReturnDto
        {
            Id = sale.Id,
            Number = sale.Number,
            CreatedAt = sale.CreatedAt,
            TotalAmount = sale.TotalAmount,
            SoldByName = sale.SoldByName,
            Type = sale.Type,
            Items = sale.SaleItems.Select(si => new ReturnableItemDto
            {
                SaleItemId = si.Id,
                ProductDisplayName = si.ProductDisplayName,
                ProductSku = si.ProductSku,
                Quantity = si.Quantity,
                ReturnableQuantity = si.Quantity,
                UnitPrice = si.UnitPrice
            }).ToList()
        };
    }
}
