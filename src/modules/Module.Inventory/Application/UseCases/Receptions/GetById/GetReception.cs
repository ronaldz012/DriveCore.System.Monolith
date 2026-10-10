using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;

namespace Module.Inventory.Application.UseCases.Receptions.GetById;

public class GetReception(IInvDbContext context)
{
    public async Task<Result<StockReceptionDetailDto>> Execute(ActorContext ctx, Guid id)
    {
        var currentBranch = ctx.BranchIds[0];
       
        var reception =  await context.StockReceptions
            .Where(r => r.Id == id &&  r.BranchId == currentBranch)
            .Select(r => new StockReceptionDetailDto
            {
                Id = r.Id,
                Number = r.Number,
                BranchId = r.BranchId,
                ProviderId = r.ProviderId,
                ProviderName = r.Provider.Name,
                ReceivedAt = r.ReceivedAt,
                Notes = r.Notes,
                Status = r.Status,
                TotalCost = r.Items.Sum(i => i.UnitCost * i.QuantityReceived),
                Items = r.Items.OrderBy(i => i.ProductVariant.Product.Name).ThenBy(i => i.ProductVariant.Color.Name).ThenBy(i => i.ProductVariant.Size.SortOrder).ThenBy(i => i.ProductVariant.Sku).Select(i => new StockReceptionItemDetailDto
                {
                    Id = i.Id,
                    Sku = i.ProductVariant.Sku,
                    ProductVariantId = i.ProductVariantId,
                    ProductId = i.ProductVariant.ProductId,
                    BrandName = i.ProductVariant.Product.Brand.Name,
                    ProductName = i.ProductVariant.Product.Name,
                    VariantDescription = i.ProductVariant.Description,
                    Size = i.ProductVariant.Size.Name,
                    Color = i.ProductVariant.Color.Name,
                    QuantityReceived = i.QuantityReceived,
                    UnitCost = i.UnitCost,
                    Subtotal = i.UnitCost * i.QuantityReceived
                }).ToList()
            })
            .FirstOrDefaultAsync();
        if(reception == null)
            return GetReceptionErrors.ReceptionNotFound;
        
        return reception;
    }
}