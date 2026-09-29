using Common.Utilities;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Application.UseCases.Liquidation;

namespace Module.Inventory.Infrastructure.Advisor;

/// <summary>
/// Fallback cuando no hay API key configurada: error controlado en vez de fallo de DI.
/// </summary>
public class MissingKeyLiquidationAdvisor : ILiquidationAdvisor
{
    public Task<Result<LiquidationAdviceDto>> AdviseAsync(
        LiquidationStateDto state, CancellationToken cancellationToken = default)
    {
        Result<LiquidationAdviceDto> result =
            new Error(ErrorCode.InvalidState, "Jev no configurado: falta la variable de entorno TypeSafeApiKey");
        return Task.FromResult(result);
    }
}
