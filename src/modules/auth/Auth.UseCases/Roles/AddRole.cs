using System;
using Auth.Contracts.Dtos.Roles;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using MapsterMapper;
using Shared.Result;

namespace Auth.UseCases.Roles;

public class AddRole(AuthDbContext dbContext, IMapper mapper)
{
    public async Task<Result<int>> Execute(CreateRoleDto dto)
    {
        var role = new Role()
        {
            Name = dto.Name,
            Description = dto.Description,
            RoleFeaturePermissions = dto.RoleModulePermissions.Select( f =>new RoleFeaturePermission()
            {
                FeatureId = f.FeatureId,
                CanCreate =  f.CanCreate,
                CanUpdate = f.CanUpdate,
                CanDelete = f.CanDelete,
                CanRead =  f.CanRead
            }).ToList()
        };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        return role.Id; 
    }
}
