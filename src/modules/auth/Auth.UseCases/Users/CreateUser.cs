using Auth.Contracts.Dtos.Users;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.UseCases.Autentication;
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
        var validation = await context.Users.AnyAsync(u => u.Email == dto.Email || u.Username == dto.Username);
        if(validation) return new Error("INVALID_OPERATION", "email or username taken");
        
 
        
        //CORRER EN PARALELO AMBAS TAREAS, VER SI MEJORA
        var branchesResult = await branchService.GetBranchesByIds(dto.BranchIds.ToList());
        var rolesExist =await ValidateRoles(dto.RoleIds.ToList());
        
        if(!branchesResult.IsSuccess) return new Error("NOT_FOUND", branchesResult.Error?.Message ?? "");
        
        if(!rolesExist.IsSuccess) return new Error("NOT_FOUND",rolesExist.Error?.Message ?? "");
        
        byte[] passwordHash, passwordSalt;
        ValidatePassword.CreatePasswordHash(dto.Password, out passwordHash, out passwordSalt);
        
        var newUser = mapper.Map<User>(dto);//propiedades simples
        newUser.PasswordHash = passwordHash;
        newUser.PasswordSalt = passwordSalt;
        newUser.Status = UserStatus.Active;
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

    private async Task<Result<bool>> ValidateRoles(List<int> roleIds)
    {
        var foundRoles = await context.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync();
        
        var foundRolesIds = foundRoles.Select(r => r.Id).ToList();
        var missingRoleIds = roleIds.Except(foundRolesIds).ToList();
        if (missingRoleIds.Any())
            return new Error("NOT_FOUND", $"roles not found, missing: {missingRoleIds}");
        
        return true;
    }
}