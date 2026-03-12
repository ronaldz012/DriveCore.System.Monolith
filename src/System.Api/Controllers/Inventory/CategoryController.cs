using System.Api.Result;
using Inventory.Contracts.Dtos.Categories;
using Inventory.UseCases.Categories;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(CategoryUseCases categoryUseCases) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
        {
            return await categoryUseCases.CreateCategory.Execute(dto).ToValueOrProblemDetails();
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] CategoryQueryDto query)
        {
            return await categoryUseCases.GetCategories.Execute(query).ToValueOrProblemDetails();
        }
    }
}
