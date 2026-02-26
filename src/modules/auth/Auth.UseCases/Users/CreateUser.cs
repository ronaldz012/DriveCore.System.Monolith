using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Dtos.Users;
using Branches.Contracts;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Users;

public class CreateUser(AuthDbContext context, IBranchService branchService, IMapper mapper)
{
    //recibir usuario con datos completos, recibir roleIds, recibir branchIds:

    public async Task<Result<bool>> Execute(CreateUserDto dto)
    {
        //CORRER EN PARALELO AMBAS TAREAS, VER SI MEJORA
        var branchesResult = await branchService.GetBranchesByIds(dto.BranchIds.ToList());
        var rolesExist =await ValidateRoles(dto.RoleIds.ToList());
        
        if(!branchesResult.IsSuccess) return new Error("NOT_FOUND", branchesResult.Error?.Message ?? "");
        
        if(!rolesExist) return new Error("NOT_FOUND","one or many roles do not exist");
        
        var newUser = mapper.Map<User>(dto);//propiedades simples
        newUser.UserBranches = dto.BranchIds.Select(branchId => new UserBranch
        {
            BranchId = branchId,
        }).ToList();
        newUser.UserRoles = dto.RoleIds.Select(roleId => new UserRole()
        {
            RoleId = roleId,
        }).ToList();
        context.Add(newUser);
        await context.SaveChangesAsync();
        return true;

    }

    private async Task<bool> ValidateRoles(List<int> roleIds)
    {
        var foundRoles = await context.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync();
        
        var foundRolesIds = foundRoles.Select(r => r.Id).ToList();
        var missingRoleIds = roleIds.Except(foundRolesIds).ToList();
        return missingRoleIds.Any(); //si hay alguno no encontrado retorna true
    }
}