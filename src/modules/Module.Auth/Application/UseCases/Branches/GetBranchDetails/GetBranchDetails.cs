using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Auth.Application.Abstraction;

namespace Module.Auth.Application.UseCases.Branches.GetBranchDetails;

public class GetBranchDetails(IAuthDbContext context)
{
    public async Task<Result<GetBranchDetailsResponse>> Execute(Guid id)
    {
        var branch = await context.Branches
            .Where(b => b.Id == id)
            .Select(b => new GetBranchDetailsResponse
            {
                Id = b.Id,
                Name = b.Name,
                Place = b.Place,
                PhoneNumber = b.PhoneNumber,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt,
                CompleteSince = b.CompleteSince,
            })
            .FirstOrDefaultAsync();

        if (branch is null)
            return GetBranchDetailsErrors.BranchNotFound;

        return branch;
    }
}
