using Inventory.Contracts.Dtos.Brands;
using Inventory.Data.Entities.Products;
using Inventory.Data.Persistence;
using Shared.Result;

namespace Inventory.UseCases.Brands;

public class CreateBrand(InvDbContext context)
{
    public async Task<Result<int>> Execute(CreateBrandDto dto)
    {
        var newBrand = new Brand
        {
            Name = dto.Name
        };  
        context.Brands.Add(newBrand);
        await context.SaveChangesAsync();
        return newBrand.Id;
    }
}