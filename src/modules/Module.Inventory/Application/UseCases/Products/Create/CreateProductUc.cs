using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.Products.Create;

public class CreateProductUc(IInvDbContext context, IProductCodeService codeService)
{
    public async Task<Result<ProductCreatedDto>> Execute(ActorContext ctx, CreateProductRequest request)
    {
        var brand = await context.Brands.FindAsync(request.BrandId);
        if (brand == null)
            return CreateProductErrors.BrandNotFound;

        var category = await context.Categories.FindAsync(request.CategoryId);
        if (category == null)
            return CreateProductErrors.CategoryNotFound;

        var duplicateName = await context.Products.AnyAsync(p =>
            p.CategoryId == request.CategoryId &&
            p.BrandId == request.BrandId &&
            p.Name.ToLower() == request.Name.ToLower());

        if (duplicateName)
            return CreateProductErrors.ProductNameAlreadyExists;

        var colorIds = request.Variants.Select(pv => pv.ColorId).Distinct().ToList();
        var colors = await context.Colors
            .Where(c => colorIds.Contains(c.Id))
            .ToListAsync();

        if (colorIds.Except(colors.Select(c => c.Id)).Any())
            return CreateProductErrors.ColorsNotFound;

        var sizeIds = request.Variants.Select(pv => pv.SizeId).Distinct().ToList();
        var sizes = await context.Sizes
            .Where(s => sizeIds.Contains(s.Id))
            .ToListAsync();

        if (sizeIds.Except(sizes.Select(s => s.Id)).Any())
            return CreateProductErrors.SizesNotFound;

        await using var tx = await context.Database.BeginTransactionAsync();
        try
        {
            var internalCode = await codeService.ReserveBrandCounter(request.BrandId, brand.Prefix);

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                CategoryId = request.CategoryId,
                BrandId = request.BrandId,
                Gender = request.Gender,
                InternalCode = internalCode,
                ProductVariantCounter = 0,
                CreatedBy = ctx.UserId,
                CreatedByName = ctx.FullName
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Ordenar antes de asignar SKUs para que el correlativo siga color -> talla
            var colorOrder = colors.ToDictionary(c => c.Id, c => c.Name);
            var sizeOrder = sizes.ToDictionary(s => s.Id, s => s.SortOrder);
            var orderedVariants = request.Variants
                .OrderBy(v => colorOrder[v.ColorId])
                .ThenBy(v => sizeOrder[v.SizeId])
                .ToList();

            var variants = new List<ProductVariant>();
            foreach (var pv in orderedVariants)
            {
                var sku = await codeService.ReserveVariantCounter(product.Id, product.InternalCode);
                variants.Add(new ProductVariant
                {
                    ProductId = product.Id,
                    ColorId = pv.ColorId,
                    SizeId = pv.SizeId,
                    Description = pv.Description,
                    Price = pv.Price,
                    Sku = sku,
                    CreatedBy = ctx.UserId,
                    CreatedByName = ctx.FullName
                });
            }

            context.ProductVariants.AddRange(variants);
            await context.SaveChangesAsync();

            await tx.CommitAsync();

            var saved = await context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductVariants)
                    .ThenInclude(pv => pv.Color)
                .Include(p => p.ProductVariants)
                    .ThenInclude(pv => pv.Size)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            if (saved == null)
                return CreateProductErrors.ProductRetrievalFailed;

            return new ProductCreatedDto
            {
                Id = saved.Id,
                InternalCode = saved.InternalCode,
                Name = saved.Name,
                BrandName = saved.Brand.Name,
                CategoryName = saved.Category.Name,
                IsActive = saved.IsActive,
                Variants = saved.ProductVariants.OrderBy(pv => pv.Color.Name).ThenBy(pv => pv.Size.SortOrder).ThenBy(pv => pv.Sku).Select(pv => new ProductVariantsCreated
                {
                    ProductVariantId = pv.Id,
                    Sku = pv.Sku,
                    Size = pv.Size.Name,
                    ColorName = pv.Color.Name
                }).ToList()
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}