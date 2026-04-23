using Auth.Contracts.Dtos.Modules;
using Auth.Contracts.Dtos.Users;
using Auth.Data.Entities;
using Branches.Contracts;
using Shared.Result;

namespace Auth.UseCases.Autentication.functions;

// La clase debe ser 'public static' para que puedas acceder a ella sin instanciarla
public static class UserMappingUtils
{
    public static List<ModulePermissionsDeductedDto> CalculateModulePermissions(List<UserBranchRole> branchRoles)
    {
        return branchRoles
            .SelectMany(ubr => ubr.Role.RoleModulePermissions)
            .GroupBy(rmp => rmp.ModuleId)
            .Select(g => new
            {
                Module = g.First().Module,
                CanRead = g.Any(rmp => rmp.CanRead),
                CanCreate = g.Any(rmp => rmp.CanCreate),
                CanUpdate = g.Any(rmp => rmp.CanUpdate),
                CanDelete = g.Any(rmp => rmp.CanDelete)
            })
            .Select(mp => new ModulePermissionsDeductedDto
            {
                Id = mp.Module.Id,
                Name = mp.Module.Name,
                Route = mp.Module.Route,
                CanRead = mp.CanRead,
                CanCreate = mp.CanCreate,
                CanUpdate = mp.CanUpdate,
                CanDelete = mp.CanDelete,
                Menus = mp.Module.Menus
                    .OrderBy(m => m.Order)
                    .Select(m => new MenuDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Route = m.Route,
                        Icon = m.Icon,
                        Order = m.Order
                    }).ToList()
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
                    Modules = CalculateModulePermissions(g.ToList())
                };
            }).ToList();
    }
}