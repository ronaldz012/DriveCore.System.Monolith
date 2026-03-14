using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Inventory.UseCases.Products;

public class ValidateProducts(InvDbContext context)
{
    public async Task<Result<bool>> Execute(List<int> productIds)
    {
        var idsFounded = await context.Products.Where(x => productIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();

        var missingIds = productIds.Except(idsFounded).ToList();
        if (missingIds.Any())
            return new Error("NOT_FOUND", $"the next Ids Where Not founded{missingIds}");
        return true;
    }
}