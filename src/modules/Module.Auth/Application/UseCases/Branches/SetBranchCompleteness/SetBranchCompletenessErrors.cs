using Common.Utilities;

namespace Module.Auth.Application.UseCases.Branches.SetBranchCompleteness;

public static class SetBranchCompletenessErrors
{
    public static readonly Error BranchNotFound = new(ErrorCode.NotFound, "Branch not found");
}
