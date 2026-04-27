using System.Api.Filters;
using System.Api.Result;
using Inventory.Contracts.Dtos.Transfers;
using Inventory.UseCases.Transfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockTransferController(StockTransferUseCases useCases) : ControllerBase
    {

        [HttpPost]
        [RequireBranch]
        public async Task<IActionResult> CreateStockTransfer([FromBody ]CreateStockTransferDto createStockTransferDto)
        {
            return await  useCases.CreateStockTransfer.Execute(createStockTransferDto).ToValueOrProblemDetails();
        }

        [HttpPost("{transferId:int}")] 
        [RequireBranch]
        public async Task<IActionResult> ResolveStockTransfer([FromRoute] int transferId, [FromBody] ResolveStockTransferDto resolveStockTransferDto)
        {
            return await useCases.ResolveStockTransfer
                .Execute(transferId, resolveStockTransferDto)
                .ToValueOrProblemDetails();
        }
    }
}
