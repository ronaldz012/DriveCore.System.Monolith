using System.Api.Attributes;
using System.Api.Result;
using Common.Contracts.authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Module.Auth.Application.Abstraction;
using Module.Inventory.Application.UseCases.Receptions;
using Module.Inventory.Application.UseCases.Receptions.Confirm;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Application.UseCases.Receptions.Get;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | Receptions")]
    [Authorize]
    public class ReceptionController(ReceptionUseCases service, ISessionStateService currentUser) : ControllerBase
    {
        [HttpPost]
        [RequireFeature("receptions", "create")]
        public async Task<IActionResult> CreateReception(CreateStockReceptionDto dto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await service.CreateReceptionUc.Execute(actorResult.Value, dto).ToValueOrProblemDetails();
        }

        [HttpGet]
        [RequireFeature("receptions", "read")]
        public async Task<IActionResult> ListReceptions([FromQuery] ReceptionQueryDto dto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await service.ListReceptions.Execute(actorResult.Value, dto).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}")]
        [RequireFeature("receptions", "read")]
        public async Task<IActionResult> GetReception([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await service.GetReception.Execute(actorResult.Value, id).ToValueOrProblemDetails();
        }

        [HttpPost("{id:guid}/confirm")]
        [RequireFeature("receptions", "update")]
        public async Task<IActionResult> Confirm([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await service.ConfirmStockReceptionUc.Execute(actorResult.Value, id).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}/labels")]
        [RequireFeature("receptions", "read")]
        public async Task<IActionResult> GetLabels([FromRoute] Guid id)
        {
            return await service.ReceptionLabels.Execute(id).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}/can-revert")]
        [RequireFeature("receptions", "delete")]
        public async Task<IActionResult> CheckCanRevert([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await service.RevertStockReception.Check(actorResult.Value, id).ToValueOrProblemDetails();
        }

        [HttpPost("{id:guid}/revert")]
        [RequireFeature("receptions", "delete")]
        public async Task<IActionResult> Revert([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await service.RevertStockReception.Execute(actorResult.Value, id).ToValueOrProblemDetails();
        }
    }
}
