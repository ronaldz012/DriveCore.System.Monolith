using System;
using Auth.Contracts.Dtos.Roles;
using Auth.Data.Persistence;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using Shared.Result;

namespace Auth.UseCases.Roles;

public class GetRole(AuthDbContext dbContext, IMapper mapper)
{

    public async Task<Result<RoleDetailsDto>> Execute(int roleId)
    {
        var role = await dbContext.Roles.Where(r => r.Id == roleId)
            .Include(r => r.RoleModulePermissions).ThenInclude(rmp => rmp.Module)
            .FirstOrDefaultAsync();
        if (role == null)
            return new Error("NOT_FOUND", "Role not found");

        return mapper.Map<RoleDetailsDto>(role);
    }
}
