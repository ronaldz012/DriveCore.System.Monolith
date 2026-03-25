using System.Api.Result;
using Inventory.Contracts.Dtos.Receptions;
using Inventory.UseCases.Receptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Inventory | Receptions")]
    public class ReceptionController(ReceptionUseCases service) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateReception(CreateStockReceptionDto dto)
        {
            return await service.CreateReceptionUc.Execute(dto).ToValueOrProblemDetails();
        }

        [HttpGet]
        public async Task<IActionResult> ListReceptions([FromQuery] ReceptionQueryDto dto)
        {
            return await service.ListReceptions.Execute(dto).ToValueOrProblemDetails();
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetReception([FromRoute] int id)
        {
            return await service.GetReception.Execute(id).ToValueOrProblemDetails();
        }
    }
}
