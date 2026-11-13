using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Edit;

public class UpdateDraftUc(
    IInvDbContext context,
    ILogger<UpdateDraftUc> logger)
{
    public async Task<Result<StockReceptionResultDto>> Execute(ActorContext ctx, Guid id, UpdateDraftDto dto)
    {
        var reception = await context.StockReceptions
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.BranchId == ctx.BranchId);

        if (reception is null)
            return EditDraftErrors.ReceptionNotFound;

        if (reception.Status != ReceptionStatus.Draft)
            return EditDraftErrors.NotDraft;

        var provider = await context.Providers
            .Where(p => p.Id == dto.ProviderId)
            .Select(p => new { p.IsActive })
            .FirstOrDefaultAsync();

        if (provider is null)
            return EditDraftErrors.ProviderNotFound;

        if (!provider.IsActive)
            return EditDraftErrors.ProviderInactive;

        var variantIds = dto.Items.Select(x => x.ProductVariantId).Distinct().ToList();

        var variants = await context.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        if (variants.Count != variantIds.Count)
            return EditDraftErrors.VariantsNotFound;

        if (variants.Any(v => !v.Product.IsActive))
            return EditDraftErrors.ProductInactive;

        try
        {
            // Full replace: draft items have no external references, so the payload is
            // the final state (borrón y cuenta nueva instead of diffing).
            reception.Items.Clear();
            foreach (var item in dto.Items)
            {
                reception.AddExistingVariant(
                    item.ProductVariantId, ctx.UserId, ctx.FullName, item.QuantityReceived, item.UnitCost);
            }

            reception.ProviderId = dto.ProviderId;
            reception.Notes = dto.Notes;
            reception.UpdatedAt = DateTime.UtcNow;
            reception.UpdatedBy = ctx.UserId;
            reception.UpdatedByName = ctx.FullName;

            await context.SaveChangesAsync();

            var result = await context.StockReceptions
                .Where(r => r.Id == reception.Id)
                .ProjectToResult()
                .FirstOrDefaultAsync();

            if (result is null)
                return EditDraftErrors.EditFailed;

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating draft reception {ReceptionId}", id);
            return EditDraftErrors.EditFailed;
        }
    }
}
