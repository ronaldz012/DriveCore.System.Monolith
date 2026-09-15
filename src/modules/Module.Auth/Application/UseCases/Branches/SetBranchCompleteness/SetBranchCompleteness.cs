using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Auth.Application.Abstraction;

namespace Module.Auth.Application.UseCases.Branches.SetBranchCompleteness;

public class SetBranchCompleteness(IAuthDbContext context)
{
    public async Task<Result<bool>> Execute(Guid id, SetBranchCompletenessRequest request)
    {
        var branch = await context.Branches.FirstOrDefaultAsync(b => b.Id == id);
        if (branch == null) return SetBranchCompletenessErrors.BranchNotFound;

        branch.MarkComplete(request.CompleteSince);

        await context.SaveChangesAsync();
        return true;
    }
}
