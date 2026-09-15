using Module.Auth.Application.UseCases.Branches.CreateBranch;
using Module.Auth.Application.UseCases.Branches.GetBranches;
using Module.Auth.Application.UseCases.Branches.GetBranchDetails;
using Module.Auth.Application.UseCases.Branches.GetBranchTypes;
using Module.Auth.Application.UseCases.Branches.UpdateBranch;
using Module.Auth.Application.UseCases.Branches.ToggleBranchStatus;
using Module.Auth.Application.UseCases.Branches.SetBranchCompleteness;
namespace Module.Auth.Application.UseCases.Branches;

public record BranchesUseCases(
    CreateBranch.CreateBranch CreateBranch,
    GetBranches.GetBranches ListBranches,
    UpdateBranch.UpdateBranch UpdateBranch,
    ToggleBranchStatus.ToggleBranchStatus ToggleBranchStatus,
    GetBranchDetails.GetBranchDetails GetBranchDetails,
    GetBranchTypes.GetBranchTypes GetBranchTypes,
    SetBranchCompleteness.SetBranchCompleteness SetBranchCompleteness);