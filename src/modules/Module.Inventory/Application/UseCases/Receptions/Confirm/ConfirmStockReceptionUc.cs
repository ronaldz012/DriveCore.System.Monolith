using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Confirm;

public class ConfirmStockReceptionUc(
    IInvDbContext context,
    ILogger<ConfirmStockReceptionUc> logger)
{
    public async Task<Result<StockReceptionResultDto>> Execute(ActorContext ctx, Guid id)
    {
        var branchId = ctx.BranchId;

        var reception = await context.StockReceptions
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.BranchId == branchId);

        if (reception is null)
            return ConfirmStockReceptionErrors.ReceptionNotFound;

        if (reception.Status != ReceptionStatus.Draft)
            return ConfirmStockReceptionErrors.NotDraft;

        if (!reception.HasAllUnitCosts())
            return ConfirmStockReceptionErrors.MissingUnitCosts;

        var variantIds = reception.Items.Select(i => i.ProductVariantId).Distinct().ToList();

        // Re-validate at confirmation time: products may have been deactivated since the draft was saved.
        var variants = await context.ProductVariants
            .Include(v => v.BranchInventories)
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        if (variants.Count != variantIds.Count)
            return ConfirmStockReceptionErrors.VariantsNotFound;

        if (variants.Any(v => !v.Product.IsActive))
            return ConfirmStockReceptionErrors.ProductInactive;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            context.StockMovements.AddRange(
                reception.Confirm(variants, branchId, ctx.UserId, ctx.FullName));

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = await context.StockReceptions
                .Where(r => r.Id == reception.Id)
                .ProjectToResult()
                .FirstOrDefaultAsync();

            if (result is null)
                return ConfirmStockReceptionErrors.ReceptionQueryFailed;

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error confirming reception {ReceptionId} for branch {BranchId}", id, branchId);
            return ConfirmStockReceptionErrors.ConfirmFailed;
        }
    }
}
