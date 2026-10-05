using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.ProductVariants.GetBySku;

public class GetProductVariantByCode(IInvDbContext context)
{
    public async Task<Result<ProductVariantBySkuDto>> Execute(ActorContext ctx, string skuRequested)
    {
        var branch = ctx.BranchIds[0];
        var result = await context.ProductVariants.Select(pv => new ProductVariantBySkuDto
            {
                Id = pv.Id,
                Sku = pv.Sku,
                Description = pv.Description,
                Size = pv.Size.Name,
                SizeId = pv.SizeId,
                ColorId = pv.ColorId,
                ColorName = pv.Color.Name,
                Price = pv.Price,
                AverageCost = pv.AverageCost,
                BranchId = branch,
                AvailableStockInBranch = pv.BranchInventories.Where(bi => bi.BranchId == branch).Select(bi => bi.Stock).FirstOrDefault(),
                ProductId= pv.ProductId,
                ProductName = pv.Product.Name,
                ProductDescription = pv.Product.Description,
                Gender = pv.Product.Gender,
                BrandName = pv.Product.Brand.Name,
                CategoryName = pv.Product.Category.Name,
                IsActive = pv.Product.IsActive,
            }

        ).FirstOrDefaultAsync(pv => pv.Sku == skuRequested);
        if(result is null) return GetProductVariantByCodeErrors.VariantNotFound;

        if (!result.IsActive)
            return GetProductVariantByCodeErrors.ProductInactive;

        result.DisplayName = ProductVariant.BuildDisplayName(
            result.BrandName, result.CategoryName, result.ProductName, result.ColorName, result.Size);

        return result;
    }
}