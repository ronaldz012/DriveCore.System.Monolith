using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;

namespace Module.Inventory.Application.UseCases.Products.Search;

public class SearchProduct(IInvDbContext context)
{
    public async Task<Result<List<ProductDto>>> Execute(string query, bool? includeInactive = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<ProductDto>();

        var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);



        var dbQuery = context.Products
            .AsNoTracking();

        if (includeInactive != true)
        {
            dbQuery = dbQuery.Where(x => x.IsActive);
        }

        foreach (var word in keywords)
        {
            var pattern = $"%{word}%";

            dbQuery = dbQuery.Where(x =>
                    EF.Functions.ILike(x.Name, pattern) ||
                    EF.Functions.ILike(x.InternalCode, pattern) || 
                    EF.Functions.ILike(x.Brand.Name, pattern));
        }

        var result = await dbQuery
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                InternalCode = x.InternalCode,
                Description = x.Description,
                CategoryName = x.Category.Name,
                BrandName = x.Brand.Name,
                BasePrice = x.BasePrice,
                Gender = x.Gender,
                IsActive = x.IsActive,
                VariantsCount = x.ProductVariants.Count(),
                CreatedAt = x.CreatedAt,
                ProductVariants = x.ProductVariants.OrderBy(y => y.Color.Name).ThenBy(y => y.Size.SortOrder).ThenBy(y => y.Sku).Select(y => new ProductVariantDto
                {
                    Id = y.Id,
                    Description = y.Description,
                    Sku = y.Sku,
                    Size = y.Size.Name,
                    SizeId = y.SizeId,
                    ColorId =  y.ColorId,
                    ColorName = y.Color.Name,
                    Price = y.Price,
                    AverageCost = y.AverageCost
                }).ToList()
            })
            .Take(10)
            .ToListAsync();

        return result;
    }
}