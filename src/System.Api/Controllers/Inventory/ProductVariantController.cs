using System.Api.Attributes;
using System.Api.Result;
using Common.Contracts.authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Module.Auth.Application.Abstraction;
using Module.Inventory.Application.UseCases.Products.Create;
using Module.Inventory.Application.UseCases.ProductVariants;
using Module.Inventory.Application.UseCases.StockMovements;
using Module.Inventory.Application.UseCases.StockMovements.Get;
using Module.Inventory.Application.UseCases.ProductVariants.Create;
using Module.Inventory.Application.UseCases.ProductVariants.PatchStock;
using Module.Inventory.Application.UseCases.ProductVariants.Update;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | ProductVariants")]
    [Authorize]
    public class ProductVariantController(ProductVariantUseCases useCases, StockMovementUseCases stockMovementUseCases, ISessionStateService currentUser) : ControllerBase
    {
        [HttpPut("{id:guid}")]
        [RequireFeature("products", "update")]
        public async Task<IActionResult> UpdateProductVariant([FromRoute] Guid id,[FromBody]UpdateProductVariantDto dto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.UpdateProductVariant.Execute(actorResult.Value, dto, id).ToValueOrProblemDetails();
        }

        [HttpPatch("{id:guid}")]
        [RequireFeature("products", "update")]
        public async Task<IActionResult> UpdateStock([FromRoute] Guid id, [FromBody] UpdateProductVariantStockDto dto)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.CorrectProductVariantStock.Execute(actorResult.Value, dto, id).ToValueOrProblemDetails();
        }
        [HttpGet]
        public async Task<IActionResult> GetVariantProductByCode([FromQuery] string request)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.GetProductVariantByCode.Execute(actorResult.Value, request).ToValueOrProblemDetails();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchVariants([FromQuery] string request)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.SearchProductVariants.Execute(actorResult.Value, request).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}/details")]
        [RequireFeature("products", "read")]
        public async Task<IActionResult> GeProductVariantById([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.GetProductVariantDetails.Execute(actorResult.Value, id).ToValueOrProblemDetails();
        }
        [HttpPost("{productId:guid}")]
        [RequireFeature("products", "update")]
        public async Task<IActionResult> CreateProductVariants([FromRoute]Guid productId, [FromBody] CreateProductVariantsRequest request)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.CreateProductVariantUc.Execute(actorResult.Value, productId, request).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}/can-delete")]
        [RequireFeature("products", "delete")]
        public async Task<IActionResult> CanDeleteVariant([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.DeleteProductVariantUc.Check(actorResult.Value, id).ToValueOrProblemDetails();
        }

        [HttpDelete("{id:guid}")]
        [RequireFeature("products", "delete")]
        public async Task<IActionResult> DeleteProductVariant([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await useCases.DeleteProductVariantUc.Execute(actorResult.Value, id).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}/movements")]
        [RequireFeature("products", "read")]
        public async Task<IActionResult> GetProductVariantMovements([FromRoute] Guid id, [FromQuery] StockMovementQueryDto query)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await stockMovementUseCases.ListStockMovements.Execute(actorResult.Value, id, query).ToValueOrProblemDetails();
        }
    }
}
