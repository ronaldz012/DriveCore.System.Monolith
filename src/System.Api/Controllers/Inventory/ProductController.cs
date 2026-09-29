using System.Api.Attributes;
using System.Api.Result;
using Common.Contracts.authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Module.Auth.Application.Abstraction;
using Module.Inventory.Application.UseCases.Products;
using Module.Inventory.Application.UseCases.Products.Create;
using Module.Inventory.Application.UseCases.Products.Get;
using Module.Inventory.Application.UseCases.Products.Update;
using Module.Inventory.Application.UseCases.Products.UpdateStatus;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | Products")]
    [Authorize]
    public class ProductController(ProductUseCases productUseCases, ISessionStateService currentUser) : ControllerBase
    {
        [HttpPost]
        [RequireFeature("products", "create")]
        public async Task<IActionResult> CreateProduct([FromBody]  CreateProductRequest request)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await productUseCases.CreateProductUc.Execute(actorResult.Value, request).ToValueOrProblemDetails();
        }

        [HttpGet]
        [RequireFeature("products", "read", true)]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryDto request)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await productUseCases.GetProductsUc.Execute(actorResult.Value, request).ToValueOrProblemDetails();
        }

        [HttpGet("Search")]
        [RequireFeature("products", "read", true)]
        public async Task<IActionResult> SearchProduct([FromQuery] string request, [FromQuery] bool? includeInactive)
        {
            return await productUseCases.SearchProducts.Execute(request, includeInactive).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}")]
        [RequireFeature("products", "read", true)]
        public async Task<IActionResult> GetProduct([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await productUseCases.ProductDetails.Execute(actorResult.Value, id).ToValueOrProblemDetails();
        }
        

        [HttpPut("{id:guid}")]
        [RequireFeature("products", "update")]
        public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto request, [FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await productUseCases.UpdateProduct.Execute(actorResult.Value, request, id).ToValueOrProblemDetails();
        }

        [HttpPatch("{id:guid}/status")]
        [RequireFeature("products", "update")]
        public async Task<IActionResult> UpdateProductStatus([FromRoute] Guid id, [FromBody] UpdateProductStatusDto request)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await productUseCases.UpdateProductStatus.Execute(actorResult.Value, id, request).ToValueOrProblemDetails();
        }

        [HttpDelete("{id:guid}")]
        [RequireFeature("products", "delete")]
        public async Task<IActionResult> DeleteProduct([FromRoute] Guid id)
        {
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await productUseCases.DeleteProduct.Execute(actorResult.Value, id).ToValueOrProblemDetails();
        }

    }
}
