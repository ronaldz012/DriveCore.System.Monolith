using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Revert;

public class RevertStockReception(
    IInvDbContext context,
    ILogger<RevertStockReception> logger)
{
    private const int MaxReceptionAgeDays = 1;

    public async Task<Result<StockReceptionRevertCheckDto>> Check(ActorContext ctx, Guid id)
    {
        var loadResult = await LoadWithVariantsAsync(ctx, id);
        if (!loadResult.IsSuccess)
            return loadResult.Error;

        var (reception, variants) = loadResult.Value;

        // Drafts never moved stock: cancelling one needs no stock or contamination checks.
        if (reception.Status == ReceptionStatus.Draft)
            return new StockReceptionRevertCheckDto
            {
                ReceptionId = id,
                CanRevert = true,
                Reason = string.Empty
            };

        var contaminated = await HasContaminatingMovementsAsync(reception);
        var blockReason = GetBlockReason(reception, variants, contaminated);

        return new StockReceptionRevertCheckDto
        {
            ReceptionId = id,
            CanRevert = blockReason == RevertBlockReason.None,
            Reason = ReasonString(blockReason)
        };
    }

    public async Task<Result<bool>> Execute(ActorContext ctx, Guid id)
    {
        var loadResult = await LoadWithVariantsAsync(ctx, id);
        if (!loadResult.IsSuccess)
            return loadResult.Error;

        var (reception, variants) = loadResult.Value;

        // Drafts never moved stock: cancelling one only changes its status.
        if (reception.Status == ReceptionStatus.Draft)
        {
            reception.Status = ReceptionStatus.Reverted;
            await context.SaveChangesAsync();
            return true;
        }

        var contaminated = await HasContaminatingMovementsAsync(reception);
        var blockReason = GetBlockReason(reception, variants, contaminated);
        if (blockReason != RevertBlockReason.None)
            return ReasonError(blockReason);

        var userId = ctx.UserId;
        var userName = ctx.FullName;
        var branchId = reception.BranchId;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            foreach (var item in reception.Items)
            {
                var variant = variants.First(v => v.Id == item.ProductVariantId);
                var stockBefore = variant.GetStockByBranch(branchId);
                variant.RevertPurchase(item.QuantityReceived, item.UnitCost);
                variant.RemoveQuantity(item.QuantityReceived, branchId);
                var stockAfter = variant.GetStockByBranch(branchId);

                context.StockMovements.Add(StockMovement.CreateReceptionRevert(
                    branchId, variant.Id, userId, userName, item.QuantityReceived, reception.Id, item.UnitCost, stockBefore, stockAfter));
            }

            reception.Status = ReceptionStatus.Reverted;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error al revertir recepción {ReceptionId} para sucursal {BranchId}", id, branchId);
            return RevertStockReceptionErrors.RevertFailed;
        }
    }

    private async Task<Result<(StockReception Reception, List<ProductVariant> Variants)>> LoadWithVariantsAsync(ActorContext ctx, Guid id)
    {
        var branchId = ctx.BranchIds[0];

        var reception = await context.StockReceptions
            .Include(r => r.Items)
            .Where(r => r.Id == id && r.BranchId == branchId)
            .FirstOrDefaultAsync();

        if (reception is null)
            return RevertStockReceptionErrors.ReceptionNotFound;

        var variantIds = reception.Items.Select(i => i.ProductVariantId).Distinct().ToList();

        var variants = variantIds.Count == 0
            ? []
            : await context.ProductVariants
                .Include(pv => pv.BranchInventories)
                .Where(pv => variantIds.Contains(pv.Id))
                .ToListAsync();

        return (reception, variants);
    }

    private static RevertBlockReason GetBlockReason(StockReception reception, List<ProductVariant> variants, bool contaminated)
    {
        if (reception.Status == ReceptionStatus.Reverted)
            return RevertBlockReason.AlreadyReverted;

        if (reception.ReceivedAt < DateTime.UtcNow.AddDays(-MaxReceptionAgeDays))
            return RevertBlockReason.Outdated;

        if (contaminated)
            return RevertBlockReason.ContaminatedBySalesOrAdjustments;

        var variantMap = variants.ToDictionary(v => v.Id);
        foreach (var item in reception.Items)
        {
            var variant = variantMap[item.ProductVariantId];
            if (!variant.HasSufficientStock(item.QuantityReceived, reception.BranchId))
                return RevertBlockReason.NotEnoughStock;
        }

        return RevertBlockReason.None;
    }

    private async Task<bool> HasContaminatingMovementsAsync(StockReception reception)
    {
        var variantIds = reception.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        if (variantIds.Count == 0)
            return false;

        var after = reception.ReceivedAt;
        var isContaminated = await context.StockMovements
            .AnyAsync(sm => variantIds.Contains(sm.ProductVariantId)
                         && sm.CreatedAt > after
                         && (sm.MovementType == MovementType.Sale || sm.MovementType == MovementType.Adjustment));

        return isContaminated;
    }

    private static string ReasonString(RevertBlockReason reason) => reason switch
    {
        RevertBlockReason.AlreadyReverted => "ALREADY_REVERTED",
        RevertBlockReason.Outdated => "OUTDATED",
        RevertBlockReason.NotEnoughStock => "NOT_ENOUGH_STOCK",
        RevertBlockReason.ContaminatedBySalesOrAdjustments => "CONTAMINATED_BY_SALES_OR_ADJUSTMENTS",
        _ => string.Empty
    };

    private static Error ReasonError(RevertBlockReason reason) => reason switch
    {
        RevertBlockReason.AlreadyReverted => RevertStockReceptionErrors.AlreadyReverted,
        RevertBlockReason.Outdated => RevertStockReceptionErrors.Outdated,
        RevertBlockReason.NotEnoughStock => RevertStockReceptionErrors.NotEnoughStock,
        RevertBlockReason.ContaminatedBySalesOrAdjustments => RevertStockReceptionErrors.ContaminatedBySalesOrAdjustments,
        _ => RevertStockReceptionErrors.RevertFailed
    };

    private enum RevertBlockReason
    {
        None,
        AlreadyReverted,
        Outdated,
        NotEnoughStock,
        ContaminatedBySalesOrAdjustments
    }
}