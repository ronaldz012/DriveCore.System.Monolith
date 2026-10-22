using System.Api.Attributes;
using System.Api.Result;
using System.Diagnostics.CodeAnalysis;
using Common.Contracts.authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Module.Auth.Application.Abstraction;
using Module.Sales.Application.UseCases;
using Module.Sales.Application.UseCases.Sales.Create;
using Module.Sales.Application.UseCases.Sales.Get;
using Module.Sales.Application.UseCases.Sales.Return;
using Module.Sales.Application.UseCases.Sales.Return.GetSaleForReturn;
using Module.Sales.Application.UseCases.Sales.Return.List;
using Module.Sales.Application.UseCases.Sales.Search;

namespace System.Api.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Sales | Sales")]
    [Authorize]
    public class SaleController(SaleUseCases saleUseCases, ISessionStateService currentUser) : ControllerBase
    {
        [HttpPost]
        [RequireFeature("pos", "create")]
        public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await saleUseCases.CreateSale.Execute(actorResult.Value, dto).ToValueOrProblemDetails();
        }
        [HttpGet]
        [RequireFeature("sales", "read")]
        public async Task<IActionResult> GetSales([FromQuery] SalesQueryDto queryDto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await saleUseCases.GetListSales.Execute(actorResult.Value, queryDto).ToValueOrProblemDetails();
        }
        [HttpGet("{id:guid}/details")]
        [RequireFeature("sales", "read")]
        public async Task<IActionResult> GetSaleDetail([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();

            var marginResult = currentUser.HasPermission("sales", "margin");
            if (!marginResult.IsSuccess) return marginResult.ToValueOrProblemDetails();

            return await saleUseCases.GetSaleDetail
                .Execute(actorResult.Value, id, marginResult.Value)
                .ToValueOrProblemDetails();
        }
        [HttpGet("returns")]
        [RequireFeature("pos", "read")]
        public async Task<IActionResult> GetReturns([FromQuery] ReturnsQueryDto queryDto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await saleUseCases.ListReturns.Execute(actorResult.Value, queryDto).ToValueOrProblemDetails();
        }
        [HttpPost("{id:guid}/returns")]
        [RequireFeature("pos", "create")]
        public async Task<IActionResult> CreateReturn([FromRoute] Guid id, [FromBody] CreateReturnDto dto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            dto.OriginalSaleId = id;
            return await saleUseCases.CreateReturn.Execute(actorResult.Value, dto).ToValueOrProblemDetails();
        }
        [HttpGet("search-by-sku")]
        [RequireFeature("pos", "read")]
        public async Task<IActionResult> SearchBySku([FromQuery] SkuSearchQueryDto queryDto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await saleUseCases.SearchSalesBySku.Execute(actorResult.Value, queryDto).ToValueOrProblemDetails();
        }
        [HttpGet("{saleId:guid}/returnable")]
        [RequireFeature("pos", "create")]
        public async Task<IActionResult> GetSaleForReturn([FromRoute] Guid saleId)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await saleUseCases.GetSaleForReturn.Execute(actorResult.Value, saleId).ToValueOrProblemDetails();
        }
    }
}
