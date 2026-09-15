using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Auth.Application.Abstraction;
using Module.Auth.Application.UseCases.Branches.GetBranches;

namespace Module.Auth.Application.UseCases.Branches.UpdateBranch;

public class UpdateBranch(IAuthDbContext context)
{
    public async Task<Result<GetBranchResponse>> Execute(Guid id, UpdateBranchRequest request)
    {
        var branch = await context.Branches.FirstOrDefaultAsync(b => b.Id == id);
        if (branch == null) return UpdateBranchErrors.BranchNotFound;

        branch.UpdateDetails(request.Name, request.Place, request.PhoneNumber, request.BranchCode);

        await context.SaveChangesAsync();

        return new GetBranchResponse
        {
            Id = branch.Id,
            Name = branch.Name,
            IsActive = branch.IsActive,
            BranchCode = branch.BranchCode,
            CompleteSince = branch.CompleteSince,
        };
    }
}
