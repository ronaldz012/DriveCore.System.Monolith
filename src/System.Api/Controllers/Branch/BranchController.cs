using System.Api.Attributes;
using System.Api.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Module.Auth.Application.Abstraction;
using Module.Auth.Application.UseCases.Branches;
using Module.Auth.Application.UseCases.Branches.CreateBranch;
using Module.Auth.Application.UseCases.Branches.GetBranches;
using Module.Auth.Application.UseCases.Branches.GetBranchDetails;
using Module.Auth.Application.UseCases.Branches.UpdateBranch;
using Module.Auth.Application.UseCases.Branches.SetBranchCompleteness;
using Module.Auth.Domain;

namespace System.Api.Controllers.Branch
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Branch")]
    [Authorize]
    public class BranchController (BranchesUseCases features, ISessionStateService currentUser): ControllerBase
    {
        [HttpPost]
        [RequireUserType(UserType.TenantAdmin)]
        public async Task<IActionResult> CreateBranch([FromBody]CreateBranchRequest request)
        { 
            var actorResult = currentUser.GetActorContext();
            if (!actorResult.IsSuccess)  return actorResult.ToValueOrProblemDetails();
            return await features.CreateBranch.Execute(actorResult.Value, request).ToValueOrProblemDetails();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetBranches([FromQuery] bool? isActive = true)
        {
            return await features.ListBranches.Execute(isActive).ToValueOrProblemDetails();
        }

        [HttpPut("{id:guid}")]
        [RequireUserType(UserType.TenantAdmin)]
        public async Task<IActionResult> UpdateBranch([FromRoute] Guid id, [FromBody] UpdateBranchRequest request)
        {
            return await features.UpdateBranch.Execute(id, request).ToValueOrProblemDetails();
        }

        [HttpPatch("{id:guid}/status")]
        [RequireUserType(UserType.TenantAdmin)]
        public async Task<IActionResult> ToggleBranchStatus([FromRoute] Guid id)
        {
            return await features.ToggleBranchStatus.Execute(id).ToValueOrProblemDetails();
        }

        [HttpPatch("{id:guid}/completeness")]
        [RequireUserType(UserType.TenantAdmin)]
        public async Task<IActionResult> SetBranchCompleteness([FromRoute] Guid id, [FromBody] SetBranchCompletenessRequest request)
        {
            return await features.SetBranchCompleteness.Execute(id, request).ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}/details")]
        [RequireUserType(UserType.TenantAdmin)]
        public async Task<IActionResult> GetBranchDetails([FromRoute] Guid id)
        {
            return await features.GetBranchDetails.Execute(id).ToValueOrProblemDetails();
        }

        [HttpGet("types")]
        [RequireUserType(UserType.TenantAdmin)]
        public async Task<IActionResult> GetBranchTypes()
        {
            return await features.GetBranchTypes.Execute().ToValueOrProblemDetails();
        }
    }
}
