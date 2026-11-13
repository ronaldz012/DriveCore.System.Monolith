using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Edit;

public class DeleteDraftUc(
    IInvDbContext context,
    ILogger<DeleteDraftUc> logger)
{
    public async Task<Result<bool>> Execute(ActorContext ctx, Guid id)
    {
        var reception = await context.StockReceptions
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id && r.BranchId == ctx.BranchId);

        if (reception is null)
            return EditDraftErrors.ReceptionNotFound;

        if (reception.Status != ReceptionStatus.Draft)
            return EditDraftErrors.NotDraft;

        try
        {
            // Hard delete: drafts have no movements or external references, and confirmed
            // or reverted receptions keep their audit trail untouched.
            context.StockReceptions.Remove(reception);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting draft reception {ReceptionId}", id);
            return EditDraftErrors.EditFailed;
        }
    }
}
