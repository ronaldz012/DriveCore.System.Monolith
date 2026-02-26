using Branches.Contracts.Dtos;
using Shared.Result;

namespace Branches.Contracts;

public interface IBranchService
{
    Task<Result<List<BranchDto>>> GetBranchesByIds(List<int> ids);
    Task<Result<List<BranchDto>>> GetBranches();
    Task<Result<bool>>  CreateBranch(CreateBranchDto request);
}