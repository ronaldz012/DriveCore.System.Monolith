using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;

namespace Module.Inventory.Application.UseCases.Receptions.GetLabels;

public class ReceptionLabels(IInvDbContext context)
{
    public async Task<Result<ReceptionLabelsDto>> Execute(Guid receptionId)
    {
        var result=  await context.StockReceptions.Where(r => r.Id == receptionId).Select(r => new ReceptionLabelsDto
        {
            ReceptionId = r.Id,
            Number = r.Number,
            ReceptionDate = r.ReceivedAt,
            Items = r.Items.OrderBy(ri => ri.ProductVariant.Product.Name).ThenBy(ri => ri.ProductVariant.Color.Name).ThenBy(ri => ri.ProductVariant.Size.SortOrder).ThenBy(ri => ri.ProductVariant.Sku).Select(ri =>new ReceptionLabelItemDto
            {
                VariantId = ri.ProductVariantId,
                Sku = ri.ProductVariant.Sku,
                Size = ri.ProductVariant.Size.Name,
                Color = ri.ProductVariant.Color.Name,
                Gender = ri.ProductVariant.Product.Gender,
                Price = ri.ProductVariant.Price,
                ProductName = ri.ProductVariant.Product.Name,
                BrandName = ri.ProductVariant.Product.Brand.Name,
                CategoryName = ri.ProductVariant.Product.Category.Name,
                Quantity = ri.QuantityReceived
            }).ToList()
        }).FirstOrDefaultAsync();

        if (result is null) return ReceptionLabelsErrors.ReceptionNotFound;
        
        return result;
    }

}
