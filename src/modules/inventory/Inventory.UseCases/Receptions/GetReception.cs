using Inventory.Contracts.Dtos.Receptions;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Inventory.UseCases.Receptions;

public class GetReception(InvDbContext context)
{
    public async Task<Result<StockReceptionDetailDto>> Execute(int id)
    {
        var reception =  await context.StockReceptions
            .Where(r => r.Id == id)
            .Select(r => new StockReceptionDetailDto
            {
                Id = r.Id,
                BranchId = r.BranchId,
                ReceivedAt = r.ReceivedAt,
                Notes = r.Notes,
                Status = r.Status,
                TotalCost = r.Items.Sum(i => i.UnitCost * i.QuantityReceived),
                Items = r.Items.Select(i => new StockReceptionItemDetailDto
                {
                    Id = i.Id,
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductVariant.Product.Name,
                    VariantDescription = i.ProductVariant.Description,
                    Size = i.ProductVariant.Size,
                    Color = i.ProductVariant.Color,
                    QuantityReceived = i.QuantityReceived,
                    UnitCost = i.UnitCost,
                    Subtotal = i.UnitCost * i.QuantityReceived
                }).ToList()
            })
            .FirstOrDefaultAsync();
        if(reception == null)
            return new Error("NOT_FOUND", "Reception not found");
        return reception;
    }
    
}