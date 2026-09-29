using Common.Contracts.authentication;
using Common.Contracts.branches;
using Common.Utilities;
using Module.Inventory.Application.Abstraction;

namespace Module.Inventory.Application.UseCases.Liquidation;

public class AdviseLiquidation(
    ILiquidationAdvisor advisor,
    IBranchService branchService)
{
    public async Task<Result<LiquidationAdviceDto>> Execute(
        ActorContext ctx, AdviseLiquidationRequest request, CancellationToken cancellationToken = default)
    {
        // TEST-ITERATION: fallback if Auth is unavailable (revert when wiring real data).
        string branchName;
        try
        {
            var branchesResult = await branchService.GetBranchesByIds([ctx.BranchId]);
            branchName = branchesResult.IsSuccess
                ? branchesResult.Value.FirstOrDefault()?.Name ?? "Unknown"
                : "Unknown";
        }
        catch
        {
            branchName = "Unknown";
        }

        // TODO: replace with BuildLiquidationState reading real data.
        var state = LiquidationStateFixtures.Sample(
            request.Dataset,
            branchName,
            ctx.TenantId.ToString(),
            request.WindowDays,
            request.EventDate,
            request.ObjectiveType,
            request.MarginFloor,
            request.UserNotes);

        return await advisor.AdviseAsync(state, cancellationToken);
    }
}
