using Inventory.Contracts.Dtos;
using Inventory.Contracts.Dtos.Products;
using Inventory.Data.Entities.Products;
using Inventory.Data.Persistence;
using Inventory.Infrastructure;
using Org.BouncyCastle.Ocsp;
using Shared.Result;

namespace Inventory.UseCases.Products;

public class CreateProduct(InvDbContext context, InventorySignalRStockNotifier notifier)
{
    public async Task<Result<bool>> Execute(CreateProductDto request)
    {
        var product = new Product
        {
            Name = request.Name,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Description = request.Description,
            BasePrice = request.BasePrice
        };
        context.Add(product);
        await context.SaveChangesAsync();
        await notifier.NotifyProductCreated(product.Name);
        return true;
    }
}