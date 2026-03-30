using Auth.Contracts.Dtos.Users;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Infrastructure.Authentication;
using Auth.UseCases.Autentication.functions;
using Branches.Contracts;
using Branches.Contracts.Dtos;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Autentication;

public class Login(AuthDbContext dbContext, ITokenGenerator tokenGenerator, IMapper mapper, IBranchService branchService)
{
    public async Task<Result<SuccessLoginDto>> Execute(LoginDto request)
    {
        var user = await dbContext.Users
            .AsSplitQuery()
            .Include(u => u.UserBranchRoles.Where(ur => ur.DeletedAt == null))
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RoleModulePermissions)
                        .ThenInclude(rmp => rmp.Module)
                            .ThenInclude(m => m.Menus)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return new Error("VALIDATION_ERROR", "Correo electrónico o contraseña incorrectos.");

        if (!ValidatePassword.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            return new Error("VALIDATION_ERROR", "Correo electrónico o contraseña incorrectos.");

        if (user.Status == UserStatus.PendingVerification)
        {
            return new SuccessLoginDto
            {
                Status = user.Status.ToString(),
                User = mapper.Map<UserDetailsDto>(user)
            };
        }

        var branchIds = user.UserBranchRoles
            .Select(ubr => ubr.BranchId)
            .Distinct()
            .ToList();

        var branchesResult = await branchService.GetBranchesByIds(branchIds);
        if (!branchesResult.IsSuccess)
            return new Error("NOT_FOUND", branchesResult.Error.Message);

        // Convertimos a diccionario para lookup eficiente
        var branchResult = await UserMappingUtils.BuildBranchAccess(user, branchService);
        if(!branchResult.IsSuccess) return new  Error("NOT_FOUND", branchResult.Error.Message);
        
        var accessToken = tokenGenerator.GenerateAccessToken(user.Id);
        var refreshToken = tokenGenerator.GenerateRefreshToken();


        return new SuccessLoginDto
        {
            Status = user.Status.ToString(),
            AuthProvider = user.AuthProvider.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = tokenGenerator.GetAccessTokenExpirationMinutes() * 60,
            User = mapper.Map<UserDetailsDto>(user),
            Branches = branchResult.Value
        };
    }
        
}