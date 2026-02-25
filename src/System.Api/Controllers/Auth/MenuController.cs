using System.Api.Result;
using Auth.Dtos.Modules;
using Auth.UseCases;
using Microsoft.AspNetCore.Mvc;


namespace System.Api.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuController(MenuUseCases menuUseCases) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            return await menuUseCases.GetMenu.Execute(id)
                                        .ToValueOrProblemDetails();
        }
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] MenuQueryDto query)
        {
            return await menuUseCases.GetAllMenus.Execute(query)
                .ToValueOrProblemDetails();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateMenuDto dto)
        {
            return await menuUseCases.AddMenu.Execute(dto)
                                            .ToValueOrProblemDetails();
        }
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] UpdateMenuDto dto)
        {
            return await menuUseCases.UpdateMenu.Execute(dto)
                                            .ToValueOrProblemDetails();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await menuUseCases.DeleteMenu.Execute(id)
                                        .ToValueOrProblemDetails();
        }
    }
}
