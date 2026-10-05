using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.ProductVariants.Search;

public class SearchProductVariants(IInvDbContext context)
{
    public async Task<Result<List<ProductVariantSearchDto>>> Execute(ActorContext ctx, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<ProductVariantSearchDto>();

        var branchId = ctx.BranchIds[0];

        var dbQuery = context.ProductVariants
            .AsNoTracking()
            .Where(pv => pv.Product.IsActive);

        foreach (var word in query.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var pattern = $"%{word}%";

            dbQuery = dbQuery.Where(pv =>
                EF.Functions.ILike(pv.Sku, pattern) ||
                EF.Functions.ILike(pv.Product.Name, pattern) ||
                EF.Functions.ILike(pv.Product.Brand.Name, pattern) ||
                EF.Functions.ILike(pv.Size.Name, pattern) ||
                EF.Functions.ILike(pv.Color.Name, pattern));
        }

        var result = await dbQuery
            .OrderBy(pv => pv.Product.Name)
            .ThenBy(pv => pv.Color.Name)
            .ThenBy(pv => pv.Size.SortOrder)
            .ThenBy(pv => pv.Sku)
            .Select(pv => new ProductVariantSearchDto
            {
                Id = pv.Id,
                Sku = pv.Sku,
                ProductId = pv.ProductId,
                ProductName = pv.Product.Name,
                BrandName = pv.Product.Brand.Name,
                CategoryName = pv.Product.Category.Name,
                Gender = pv.Product.Gender,
                ColorId = pv.ColorId,
                ColorName = pv.Color.Name,
                SizeId = pv.SizeId,
                Size = pv.Size.Name,
                Price = pv.Price,
                AvailableStockInBranch = pv.BranchInventories
                    .Where(bi => bi.BranchId == branchId)
                    .Select(bi => bi.Stock)
                    .FirstOrDefault()
            })
            .Take(10)
            .ToListAsync();

 

        return result;
    }
}