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
        var  result = await context.Products.Where(x => EF.Functions.ILike(x.Name, $"%{query}%"))
            .Select(x => new ProductDto()
            {
                Id = x.Id,
                Name = x.Name,
                Description =  x.Description,
                Category = x.Category.Name,
                Brand = x.Brand.Name,
            }).Take(10).ToListAsync();
        return result;
    }
}