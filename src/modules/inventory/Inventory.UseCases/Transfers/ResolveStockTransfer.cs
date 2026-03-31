using Inventory.Contracts.Dtos.Transfers;
using Inventory.Data.Entities.Inventory;
using Inventory.Data.Entities.Transfers;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;
using Shared.Services;

namespace Inventory.UseCases.Transfers;

public class ResolveStockTransfer(InvDbContext context, ICurrentUser  currentUser)
{
    public async Task<Result<StockTransferDetailDto>> Execute(int transferId, ResolveStockTransferDto dto)
    {
        var toBranchId = currentUser.BranchIds[0];

        var transfer = await context.StockTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == transferId);

        if (transfer == null)
            return new Error("NOT_FOUND", "Transfer not found");

        // Validar que quien resuelve es la sucursal destino
        if (transfer.ToBranchId != toBranchId)
            return new Error("FORBIDDEN", "Only the destination branch can resolve this transfer");

        if (transfer.Status != TransferStatus.Pending)
            return new Error("INVALID_OPERATION", $"Transfer is already {transfer.Status}");

        if (!dto.Accepted)
        {
            transfer.Reject(currentUser.UserId, dto.Notes);
            await context.SaveChangesAsync();
            return await GetDetail(transferId);
        }

        // Validar stock de nuevo por si cambió desde que se creó
        var variantIds = transfer.Items.Select(x => x.ProductVariantId).ToList();
        var inventories = await context.BranchInventories
            .Where(bi => bi.BranchId == transfer.FromBranchId && variantIds.Contains(bi.ProductVariantId))
            .ToListAsync();

        var insufficientStock = transfer.Items
            .Where(item => inventories.First(inv => inv.ProductVariantId == item.ProductVariantId).Stock < item.QuantityRequested)
            .ToList();

        if (insufficientStock.Count != 0)
            return new Error("INVALID_OPERATION", "Insufficient stock in origin branch, transfer cannot be completed");

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            transfer.Accept(currentUser.UserId, dto.Notes);
            var productVariants = await context.ProductVariants
                .Include(pv => pv.BranchInventories)
                .Where(pv => variantIds.Contains(pv.Id))
                .ToListAsync();
            foreach (var item in transfer.Items)
            {
                var productVariant = productVariants.First(pv => pv.Id == item.ProductVariantId);

                productVariant.UpdateQuantity(-item.QuantityRequested, transfer.FromBranchId);
                productVariant.UpdateQuantity(item.QuantityRequested, transfer.ToBranchId);

                // StockMovements
                var (movOut, movIn) = StockMovement.CreateTransfer(
                    transfer.FromBranchId,
                    transfer.ToBranchId,
                    item.ProductVariantId,
                    currentUser.UserId,
                    item.QuantityRequested
                );
                context.StockMovements.Add(movOut);
                context.StockMovements.Add(movIn);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await GetDetail(transferId);
    }

    private async Task<StockTransferDetailDto> GetDetail(int transferId)
    {
        return await context.StockTransfers
            .Where(t => t.Id == transferId)
            .Select(t => new StockTransferDetailDto
            {
                Id = t.Id,
                FromBranchId = t.FromBranchId,
                ToBranchId = t.ToBranchId,
                RequestedByUserId = t.RequestedByUserId,
                AcceptedByUserId = t.AcceptedByUserId,
                Status = t.Status,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
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