using Common.Utilities;
using Module.Inventory.Application.UseCases.Liquidation;

namespace Module.Inventory.Application.Abstraction;

public interface ILiquidationAdvisor
{
    Task<Result<LiquidationAdviceDto>> AdviseAsync(
        LiquidationStateDto state, CancellationToken cancellationToken = default);
}
