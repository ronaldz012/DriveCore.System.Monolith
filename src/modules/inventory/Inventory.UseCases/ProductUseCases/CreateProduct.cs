using Inventory.Contracts.Dtos;
using Inventory.Data.Entities.Products;
using Inventory.Data.Persistence;
using Shared.Result;

namespace Inventory.UseCases.ProductUseCases;

public class CreateProduct(InvDbContext context)
{
    public async Task<Result<bool>> Execute(CreateProductDto request)
    {
        var product = new Product
        {
            Name = request.Name,
            CategoryId = request.CategoryId,
            Description = request.Description
        };
        context.Add(product);
        await context.SaveChangesAsync();
        return true;
    }
}