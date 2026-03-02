using System.Api.Result;
using Inventory.Contracts.Dtos;
using Inventory.UseCases;
using Inventory.UseCases.ProductUseCases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | Products")]
    public class InventoryController(InvUseCases invUseCases) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductDto request)
        {
            return await invUseCases.CreateProduct.Execute(request).ToValueOrProblemDetails();
        }
    }
}
