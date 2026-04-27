using Auth.Contracts.Dtos.Users;
using Auth.Contracts.Interfaces;
using Auth.Data.Persistence;
using Auth.UseCases.Autentication.functions;
using Branches.Contracts;
using Branches.module.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;
using Shared.Services;

namespace Auth.UseCases.Autentication;

public class AutenticateMe(AuthDbContext context, ICurrentUser currentUser, IMapper mapper, IBranchService branchService) : IAuthenticateMe
{
    //sacar el token, leer el token y sacar id, devolver data no sensible (Reutilizar dtos )
    public async Task<Result<SuccessLoginDto>> Execute()
    {
        var userId = currentUser.UserId;
        var user = await context.Users
            .AsSplitQuery()
            .Include(u => u.UserBranchRoles.Where(ur => ur.DeletedAt == null))
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RoleFeaturePermissions)
                        .ThenInclude(rmp => rmp.Feature)
                            .ThenInclude(f => f.Module)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return new Error("NOT_FOUND", "Usuario no encontrado.");

        var branchIds = user.UserBranchRoles
            .Select(ubr => ubr.BranchId)
            .Distinct()
            .ToList();

        var branchResult = await UserMappingUtils.BuildBranchAccess(user, branchService);
        if (!branchResult.IsSuccess) return new Error("NOT_FOUND", branchResult.Error.Message);

        return new SuccessLoginDto
        {
            Status = user.Status.ToString(),
            AuthProvider = user.AuthProvider.ToString(),
            Branches = branchResult.Value,
            User = mapper.Map<UserDetailsDto>(user)
        };
    }


}