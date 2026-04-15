using Inventory.Contracts.Dtos.Products;
using Inventory.Data.Entities.Products;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Inventory.UseCases.Products;

public class SearchProduct(InvDbContext context)
{
    public async Task<Result<List<ProductDto>>> Execute(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<ProductDto>();

        var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Identificamos si alguna palabra coincide con nuestros Enums
        Gender? genderFilter = null;
        if (keywords.Any(k => k.Contains("hom") || k == "male")) genderFilter = Gender.Male;
        else if (keywords.Any(k => k.Contains("muj") || k == "fema")) genderFilter = Gender.Female;
        else if (keywords.Any(k => k.Contains("uni"))) genderFilter = Gender.Unisex;

        var dbQuery = context.Products
            .Include(x => x.Brand)
            .Include(x => x.Category)
            .AsNoTracking();
        foreach (var word in keywords)
        {
            var pattern = $"%{word}%";

            dbQuery = dbQuery.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                EF.Functions.ILike(x.Brand.Name, pattern) ||
                EF.Functions.ILike(x.Category.Name, pattern) ||
                (genderFilter.HasValue && x.Gender == genderFilter.Value));
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
                ProductVariants = x.ProductVariants.Select(y => new ProductVariantDto
                {
                    Id = y.Id,
                    Description = y.Description,
                    Sku =  y.Sku,
                    Size = y.Size,
                    Color = y.Color,
                    Price = y.Price
                }).ToList()
            })
            .Take(10)
            .ToListAsync();

        return result;
    }
}