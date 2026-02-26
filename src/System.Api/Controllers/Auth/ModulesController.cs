using System.Api.Result;
using Auth.Dtos.Modules;
using Auth.UseCases;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Authentication | Modules")]
    public class ModulesController(ModulesUseCases modulesUseCases) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> AddModule([FromBody] CreateModuleDto dto)
        {
            return await modulesUseCases.AddModule.Execute(dto)
            .ToValueOrProblemDetails();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetModule(int id)
        {
            return await modulesUseCases.GetModule.Execute(id)
            .ToValueOrProblemDetails();
        }
        [HttpGet]
        public async Task<IActionResult> GetAllModules([FromQuery] ModuleQueryDto queryDto)
        {
            return await modulesUseCases.GetAllModules.Execute(queryDto)
            .ToValueOrProblemDetails();
        }
    }
}
