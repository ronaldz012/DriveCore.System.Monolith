using System.Api.Result;
using Inventory.Contracts.Dtos.Brands;
using Inventory.UseCases.Brands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | Brands")]
    public class BrandController(BrandUseCases service) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateBrand([FromBody] CreateBrandDto dto)
        {
            return await service.CreateBrand.Execute(dto).ToValueOrProblemDetails();
        }

        [HttpGet]
        public async Task<IActionResult> GetBrands([FromQuery] QueryBrandDto query)
        {
            return await  service.GetBrands.Execute(query).ToValueOrProblemDetails();
        }
    }
}
