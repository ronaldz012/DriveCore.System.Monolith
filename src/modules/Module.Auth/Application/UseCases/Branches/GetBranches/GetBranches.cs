using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Auth.Application.Abstraction;
using Module.Auth.Application.UseCases.Branches.CreateBranch;

namespace Module.Auth.Application.UseCases.Branches.GetBranches;

public class GetBranches(IAuthDbContext context)
{
    public async Task<Result<List<GetBranchResponse>>> Execute(bool? isActive = true)
    {
        var query = context.Branches.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        return await query.Select(x => new GetBranchResponse
        {
            Id = x.Id,
            Name = x.Name,
            IsActive = x.IsActive,
            Place = x.Place,
            BranchCode = x.BranchCode,
            CompleteSince = x.CompleteSince,
        }).ToListAsync();
    }
}