using Inventory.Contracts.Dtos.Transfers;
using Inventory.Data.Entities.Transfers;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;
using Shared.Services;

namespace Inventory.UseCases.Transfers;

public class CreateTranfer(InvDbContext context,ICurrentUser currentUser)
{
     public async Task<Result<StockTransferDetailDto>> Execute(CreateStockTransferDto dto)
    {
        var fromBranchId = currentUser.BranchIds[0];

        // Validar que no es la misma sucursal
        if (fromBranchId == dto.ToBranchId)
            return new Error("INVALID_OPERATION", "Cannot transfer to the same branch");

        // Validar stock suficiente en origen
        var variantIds = dto.Items.Select(x => x.ProductVariantId).ToList();
        var inventories = await context.BranchInventories
            .Where(bi => bi.BranchId == fromBranchId && variantIds.Contains(bi.ProductVariantId))
            .ToListAsync();

        var missingVariants = variantIds.Except(inventories.Select(x => x.ProductVariantId)).ToList();
        if (missingVariants.Count != 0)
            return new Error("NOT_FOUND", $"Variants not found in branch: {string.Join(", ", missingVariants)}");

        var insufficientStock = dto.Items
            .Where(item => inventories.First(inv => inv.ProductVariantId == item.ProductVariantId).Stock < item.QuantityRequested)
            .ToList();
        if (insufficientStock.Count != 0)
            return new Error("INVALID_OPERATION", "Insufficient stock for some variants");

        // Crear transferencia
        var transfer = new StockTransfer
        {
            FromBranchId = fromBranchId,
            ToBranchId = dto.ToBranchId,
            RequestedByUserId = currentUser.UserId,
            Notes = dto.Notes
        };

        foreach (var item in dto.Items)
        {
            transfer.Items.Add(new StockTransferItem
            {
                ProductVariantId = item.ProductVariantId,
                QuantityRequested = item.QuantityRequested
            });
        }

        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync();

        return await context.StockTransfers
            .Where(t => t.Id == transfer.Id)
            .Select(t => new StockTransferDetailDto
            {
                Id = t.Id,
                FromBranchId = t.FromBranchId,
                ToBranchId = t.ToBranchId,
                RequestedByUserId = t.RequestedByUserId,
                Status = t.Status,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt,
                Items = t.Items.Select(i => new StockTransferItemDetailDto
                {
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductVariant.Product.Name,
                    VariantDescription = i.ProductVariant.Description,
                    Size = i.ProductVariant.Size,
                    Color = i.ProductVariant.Color,
                    QuantityRequested = i.QuantityRequested
                }).ToList()
            })
            .FirstAsync();
    }
}