using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;

namespace Module.Inventory.Application.UseCases.Products.Update;

public class UpdateProduct(IInvDbContext context)
{
    public async Task<Result<bool>> Execute(ActorContext ctx, UpdateProductDto dto, Guid id)
    {
        var product = await context.Products.FindAsync(id);
        if(product == null) return 
            UpdateProductErrors.ProductNotFound;

        if (dto.Name is not null)
        {
            var effectiveCategoryId = dto.CategoryId ?? product.CategoryId;
            var normalizedName = dto.Name.Trim().ToLowerInvariant();
            var duplicateName = await context.Products.AnyAsync(p =>
                p.Id != id &&
                p.CategoryId == effectiveCategoryId &&
                p.BrandId == product.BrandId &&
                p.Name.ToLower() == normalizedName);

            if (duplicateName)
                return UpdateProductErrors.ProductNameAlreadyExists;
        }

        product.Name = dto.Name != null ? dto.Name.Trim() : product.Name;
        product.Description = dto.Description != null ? dto.Description.Trim() : product.Description;
        product.Gender = dto.Gender ?? product.Gender;
        product.CategoryId = dto.CategoryId ?? product.CategoryId;
        product.UpdatedBy = ctx.UserId;
        product.UpdatedByName = ctx.FullName;
        product.UpdatedAt = DateTime.UtcNow;

        // Bulk de precios: se valida todo antes de mutar nada (todo o nada).
        if (dto.VariantPrices is { Count: > 0 })
        {
            var variantIds = dto.VariantPrices.Select(v => v.VariantId).ToList();

            var variants = await context.ProductVariants
                .Where(pv => pv.ProductId == id && variantIds.Contains(pv.Id))
                .ToListAsync();

            if (variants.Count != variantIds.Count)
                return UpdateProductErrors.VariantNotFound;

            foreach (var item in dto.VariantPrices)
            {
                var variant = variants.First(pv => pv.Id == item.VariantId);
                variant.Price = item.Price;
                variant.UpdatedBy = ctx.UserId;
                variant.UpdatedByName = ctx.FullName;
                variant.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
        return true;

    }
}