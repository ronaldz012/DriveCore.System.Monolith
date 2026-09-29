using System.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Module.Inventory.Application.UseCases.Liquidation;
using Module.Inventory.Infrastructure.Advisor;

namespace System.Api.Controllers.Inventory
{
    // TEST-ITERATION: controller separado sin dependencias de tenant/DB para probar
    // contra Jev con la fixture. Al cablear datos reales, mover el endpoint a
    // ProductController con [Authorize] + RequireFeature y eliminar este controller.
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | Liquidation")]
    [AllowAnonymous]
    public class LiquidationController(AdviseLiquidation adviseLiquidation, BrandAudienceProber brandAudienceProber) : ControllerBase
    {
        [HttpPost("advise")]
        [AllowAnonymous]
        public async Task<IActionResult> AdviseLiquidation([FromBody] AdviseLiquidationRequest request, CancellationToken cancellationToken)
        {
            var fakeCtx = new Common.Contracts.authentication.ActorContext(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                "Test User",
                Guid.Parse("00000000-0000-0000-0000-000000000003"),
                [Guid.Parse("00000000-0000-0000-0000-000000000003")]);
            return await adviseLiquidation.Execute(fakeCtx, request, cancellationToken).ToValueOrProblemDetails();
        }

        // EXPERIMENT: does Jev know who buys a given brand, or does it need our data?
        [HttpPost("brand-probe")]
        [AllowAnonymous]
        public async Task<IActionResult> ProbeBrandAudience(CancellationToken cancellationToken)
        {
            return await brandAudienceProber.ProbeAsync(cancellationToken).ToValueOrProblemDetails();
        }
    }
}
