using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Edit;

public class AddItemsToDraftUc(
    IInvDbContext context,
    ILogger<AddItemsToDraftUc> logger)
{
    public async Task<Result<StockReceptionResultDto>> Execute(ActorContext ctx, Guid id, AddDraftItemsDto dto)
    {
        var reception = await context.StockReceptions
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.BranchId == ctx.BranchId);

        if (reception is null)
            return EditDraftErrors.ReceptionNotFound;

        if (reception.Status != ReceptionStatus.Draft)
            return EditDraftErrors.NotDraft;

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
            foreach (var item in dto.Items)
            {
                var existing = reception.Items.FirstOrDefault(i => i.ProductVariantId == item.ProductVariantId);

                if (existing is null)
                {
                    reception.AddExistingVariant(
                        item.ProductVariantId, ctx.UserId, ctx.FullName, item.QuantityReceived, item.UnitCost);
                }
                else
                {
                    // Upsert: adding a variant that is already in the draft sums the quantities
                    // and refreshes the cost only when a positive one is provided.
                    existing.QuantityReceived += item.QuantityReceived;
                    if (item.UnitCost > 0)
                        existing.UnitCost = item.UnitCost;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = ctx.UserId;
                    existing.UpdatedByName = ctx.FullName;
                }
            }

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
            logger.LogError(ex, "Error adding items to draft reception {ReceptionId}", id);
            return EditDraftErrors.EditFailed;
        }
    }
}
