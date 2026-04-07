using Inventory.Contracts.Dtos.Products;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Inventory.UseCases.Products;

public class SearchProduct(InvDbContext context)
{
    public async Task<Result<List<ProductDto>>> Execute(string query)
    {
        var result = await context.Products.Where(x => EF.Functions.ILike(x.Name, $"%{query}%"))
            .Select(x => new ProductDto()
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                CategoryName = x.Category.Name,
                BrandName = x.Brand.Name,
                BasePrice = x.BasePrice,
                Gender =  x.Gender,
                ProductVariants = x.ProductVariants.Select(y => new ProductVariantDto
                {
                    Id = y.Id,
                    Description = y.Description,
                    Size = y.Size,
                    Color = y.Color,
                    Price = y.Price

                }).ToList()
            }).Take(10).ToListAsync();
        return result;
    }
}