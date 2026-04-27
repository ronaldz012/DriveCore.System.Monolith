using System.Api.Filters;
using System.Api.Result;
using Inventory.Contracts.Dtos;
using Inventory.Contracts.Dtos.Products;
using Inventory.UseCases;
using Inventory.UseCases.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | Products")]
    [Authorize]
    public class ProductController(ProductUseCases productUseCases) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto request)
        {
            return await productUseCases.CreateProduct.Execute(request).ToValueOrProblemDetails();
        }

        [HttpGet]
        [RequireBranch]
        public async Task<IActionResult> GetProducts([FromQuery] ProductQueryDto request)
        {
            return await productUseCases.GetProducts.Execute(request).ToValueOrProblemDetails();
        }

        [HttpGet("Search")]
        public async Task<IActionResult> SearchProduct([FromQuery] string request)
        {
            return await productUseCases.SearchProducts.Execute(request).ToValueOrProblemDetails();
        }
    }
}
