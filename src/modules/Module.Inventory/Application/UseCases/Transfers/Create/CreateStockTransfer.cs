using Common.Contracts.authentication;
using Common.Domain.Documents;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Transfers;

namespace Module.Inventory.Application.UseCases.Transfers.Create;

public class CreateStockTransfer(IInvDbContext context, IInventoryNumberGenerator numberGenerator)
{
    public async Task<Result<Guid>> Execute(ActorContext ctx, CreateStockTransferDto dto)
    {
        var fromBranchId = ctx.BranchId;

        // Validar que no es la misma sucursal
        if (fromBranchId == dto.ToBranchId)
            return CreateStockTransferErrors.SameBranchTransfer;

        // Validar stock suficiente en origen
        var variantIds = dto.Items.Select(x => x.ProductVariantId).ToList();
        var inventories = await context.BranchInventories
            .Where(bi => bi.BranchId == fromBranchId && variantIds.Contains(bi.ProductVariantId))
            .ToListAsync();

        var missingVariants = variantIds.Except(inventories.Select(x => x.ProductVariantId)).ToList();
        if (missingVariants.Count != 0)
            return CreateStockTransferErrors.VariantsNotFoundInBranch;

        var inactiveVariants = await context.ProductVariants
            .Where(v => variantIds.Contains(v.Id) && !v.Product.IsActive)
            .AnyAsync();
        if (inactiveVariants)
            return CreateStockTransferErrors.ProductInactive;

        var insufficientStock = dto.Items
            .Where(item => inventories.First(inv => inv.ProductVariantId == item.ProductVariantId).Stock < item.QuantityRequested)
            .ToList();
        if (insufficientStock.Count != 0)
            return CreateStockTransferErrors.InsufficientStock;

        // Crear transferencia
        var transfer = new StockTransfer
        {
            FromBranchId = fromBranchId,
            ToBranchId = dto.ToBranchId,
            RequestedByUserId = ctx.UserId,
            Notes = dto.Notes,
            CreatedBy = ctx.UserId,
            CreatedByName = ctx.FullName
        };

        foreach (var item in dto.Items)
        {
            transfer.Items.Add(new StockTransferItem
            {
                ProductVariantId = item.ProductVariantId,
                QuantityRequested = item.QuantityRequested,
                CreatedBy = ctx.UserId,
                CreatedByName = ctx.FullName
            });
        }

 
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            transfer.Number = await numberGenerator.NextAsync(context.Database, ctx.TenantId, InventoryCounterKey.Transfer);

            context.StockTransfers.Add(transfer);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return transfer.Id;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
