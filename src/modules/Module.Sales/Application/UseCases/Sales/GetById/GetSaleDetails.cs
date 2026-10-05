using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;

namespace Module.Sales.Application.UseCases.Sales.GetById;

public class GetSaleDetail(ISalesDbContext context)
{
    public async Task<Result<SaleDetailDto>> Execute(ActorContext ctx, Guid saleId)
    {
        var currentBranch = ctx.BranchIds[0];

        var saleDetail = await context.Sales
            .AsNoTracking()
            .Where(s => s.Id == saleId && s.BranchId == currentBranch)
            .Select(s => new SaleDetailDto
            {
                Id = s.Id,
                Number = s.Number,
                BranchId = s.BranchId,
                SoldById = s.SoldById,
                SoldByName = s.SoldByName,
                Type = s.Type,
                DocumentType = s.DocumentType,
                PaymentMethod = s.PaymentMethod,
                TransactionCode = s.TransactionCode,
                TotalAmount = s.TotalAmount,
                InvoiceNumber = s.InvoiceNumber,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                OriginalSaleId = s.OriginalSaleId,
                TotalItems = s.SaleItems.Sum(si => si.Quantity),

                Items = s.SaleItems.Select(si => new SaleItemDetailDto
                {
                    Id = si.Id,
                    ProductVariantId = si.ProductVariantId,
                    ProductDisplayName = si.ProductDisplayName,
                    ProductSku = si.ProductSku,
                    UnitPrice = si.UnitPrice,
                    UnitCost = si.UnitCost,
                    Quantity = si.Quantity,
                    DiscountAmount = si.DiscountAmount,
                    FinalPrice = si.FinalPrice,
                    ReturnedQuantity = (int)si.ChildReturns.Sum(cr => -cr.Quantity)
                }).ToList(),

                Returns = s.Returns.Select(r => new SaleRefundDto
                {
                    Id = r.Id,
                    Number = r.Number,
                    CreatedAt = r.CreatedAt,
                    TotalAmount = r.TotalAmount
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (saleDetail == null)
            return new Error(ErrorCode.NotFound, "La venta no existe o no tiene permisos para verla.");

        return saleDetail;
    }
}