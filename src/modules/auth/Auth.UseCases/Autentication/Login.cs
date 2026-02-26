using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Dtos.Users;
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
            .Include(u => u.UserBranches)
            .Include(u => u.UserRoles.Where(ur => ur.DeletedAt == null))
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RoleModulePermissions)
                        .ThenInclude(rmp => rmp.Module)
                            .ThenInclude(m => m.Menus)
            .FirstOrDefaultAsync(u => u.Email == request.Email);
        

        if (user == null)
        {
            return new Error("VALIDATION_ERROR", "Correo electrónico o contraseña incorrectos.");
        }
        
        Result<List<BranchDto>> branches = await branchService.GetBranchesByIds(user.UserBranches.Select(b => b.BranchId).ToList());
        if(!branches.IsSuccess) return new Error("NOT_FOUND", branches.Error.Message);

        if (!ValidatePassword.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return new Error("VALIDATION_ERROR", "Correo electrónico o contraseña incorrectos.");
        }
        //DEVOLVIENDO SI FALTA VERIFICACION SOLO DATOS BASICOS Y STATUS
        if(user.Status == UserStatus.PendingVerification)
        {
            return new SuccessLoginDto
            {
                Status = user.Status.ToString(),
                User = mapper.Map<UserDetailsDto>(user)
            };
        }
        var accessToken = tokenGenerator.GenerateAccessToken(user.Id);
        var refreshToken = tokenGenerator.GenerateRefreshToken();

        var roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct()
            .OrderBy(r => r)
            .ToList();

        var modules = UserMappingUtils.BuildUserModulesWithMenus(user);

        var successLoginDto = new SuccessLoginDto
        {
            Status = user.Status.ToString(),
            AuthProvider = user.AuthProvider.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = tokenGenerator.GetAccessTokenExpirationMinutes() * 60,
            Roles = roles,
            Modules = modules,
            User = mapper.Map<UserDetailsDto>(user),
            Branches = mapper.Map<List<AvailableBranchesDto>>(branches.Value) // not null, we ask above
        };

        return successLoginDto;
    }
    
}