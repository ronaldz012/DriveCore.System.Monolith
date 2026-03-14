using Inventory.Contracts.Dtos.Receptions;
using Inventory.Data.Persistence;
using Inventory.UseCases.Products;
using Shared.Result;

namespace Inventory.UseCases.Receptions;

public class CreateReceptionUC(InvDbContext context, ProductUseCases productUseCases)
{
    public async Task<Result<bool>> Execute(CreateStockReceptionDto dto)
    {
        //VALIDATE ALL PRODUCS REFERENCED
        List<int> productIds = dto.Items
            .Select(x => x.ProductId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value) 
            .ToList();
        var idsOk= await productUseCases.ValidateProducts.Execute(productIds);
        if (!idsOk.IsSuccess)return idsOk;
         
        //VALIDATE ALL PRODUCTSVARIANTS REFERENCED
        return true;
    }
}