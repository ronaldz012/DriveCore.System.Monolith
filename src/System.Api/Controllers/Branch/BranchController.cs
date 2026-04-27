using System.Api.Result;
using Branches.Contracts;
using Branches.Contracts.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace System.Api.Controllers.Branch
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Branch")]
    public class BranchController (IBranchService branchService): ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateBranch([FromBody]CreateBranchDto request)
        { 
            return await branchService.CreateBranch(request).ToValueOrProblemDetails();
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            return await branchService.GetBranches().ToValueOrProblemDetails();
        }
    }
}
