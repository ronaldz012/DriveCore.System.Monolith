using Auth.Data.Entities;
using Auth.Dtos.Modules;
using Auth.Dtos.Users;

namespace Auth.UseCases.Autentication.functions;

// La clase debe ser 'public static' para que puedas acceder a ella sin instanciarla
public static class UserMappingUtils
{
    public static List<ModulePermissionsDeductedDto> BuildUserModulesWithMenus(User user)
    {
        var modulePermissions = user.UserRoles
            .SelectMany(ur => ur.Role.RoleModulePermissions)
            .GroupBy(rmp => rmp.ModuleId)
            .Select(g => new
            {
                Module = g.First().Module,
                CanRead = g.Any(rmp => rmp.CanRead),
                CanCreate = g.Any(rmp => rmp.CanCreate),
                CanUpdate = g.Any(rmp => rmp.CanUpdate),
                CanDelete = g.Any(rmp => rmp.CanDelete)
            })
            .ToList();

        return modulePermissions.Select(mp => new ModulePermissionsDeductedDto
        {
            Id = mp.Module.Id,
            Name = mp.Module.Name,
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
                    Icon = m.Icon,
                    Order = m.Order
                })
                .ToList()
        }).ToList();
    }
}