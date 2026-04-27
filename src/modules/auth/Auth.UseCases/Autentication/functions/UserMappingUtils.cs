using Auth.Contracts.Dtos.Users;
using Auth.Data.Entities;
using Branches.Contracts;
using Shared.Result;

namespace Auth.UseCases.Autentication.functions;

// La clase debe ser 'public static' para que puedas acceder a ella sin instanciarla
public static class UserMappingUtils
{
    public static List<FeaturePermissionsDeductedDto> CalculateFeaturePermissions(List<UserBranchRole> branchRoles)
    {
        return branchRoles
            .SelectMany(ubr => ubr.Role.RoleFeaturePermissions)
            .GroupBy(rmp => rmp.FeatureId)
            .Select(g => new
            {
                Feature = g.First().Feature,
                CanRead = g.Any(rmp => rmp.CanRead),
                CanCreate = g.Any(rmp => rmp.CanCreate),
                CanUpdate = g.Any(rmp => rmp.CanUpdate),
                CanDelete = g.Any(rmp => rmp.CanDelete)
            })
            .Select(mp => new FeaturePermissionsDeductedDto
            {
                Id = mp.Feature.Id,
                Name = mp.Feature.Name,
                Route = mp.Feature.Route,
                ModuleId =  mp.Feature.ModuleId,
                ModuleName = mp.Feature.Module.Name,
                CanRead = mp.CanRead,
                CanCreate = mp.CanCreate,
                CanUpdate = mp.CanUpdate,
                CanDelete = mp.CanDelete,
            }).ToList();
    }
    public static async Task<Result<List<BranchAccessDto>>> BuildBranchAccess(
        User user,
        IBranchService branchService)
    {
        var branchIds = user.UserBranchRoles
            .Select(ubr => ubr.BranchId)
            .Distinct()
            .ToList();

        var branchesResult = await branchService.GetBranchesByIds(branchIds);
        if (!branchesResult.IsSuccess)
            return new Error("NOT_FOUND", branchesResult.Error.Message);

        var branchesById = branchesResult.Value.ToDictionary(b => b.Id);

        return user.UserBranchRoles
            .GroupBy(ubr => ubr.BranchId)
            .Select(g =>
            {
                var branch = branchesById[g.Key];
                return new BranchAccessDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.Name,
                    Roles = g.Select(ubr => new RoleDto
                    {
                        Id = ubr.Role.Id,
                        Name = ubr.Role.Name
                    }).ToList(),
                    Features = CalculateFeaturePermissions(g.ToList())
                };
            }).ToList();
    }
}