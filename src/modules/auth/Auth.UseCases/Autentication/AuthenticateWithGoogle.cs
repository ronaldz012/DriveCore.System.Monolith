using Auth.Contracts.Dtos.Modules;
using Auth.Contracts.Dtos.Users;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Infrastructure.Authentication;
using Auth.UseCases.Autentication.functions;
using Branches.Contracts;
using Branches.module.Services;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Autentication;

public class AuthenticateWithGoogle(AuthDbContext dbContext,
    RegisterUser registerUser, 
    IMapper mapper, IGoogleTokenValidator googleTokenValidator,
    ITokenGenerator tokenGenerator,
    IBranchService branchService)
{
    public async Task<Result<SuccessLoginDto>> Execute(string idToken)
    {
        // Validar token de Google
        var googleUserResult = await googleTokenValidator.ValidateTokenAsync(idToken);
        if (!googleUserResult.IsSuccess)
            return googleUserResult.Error!; // fix after mvp

        var googleUser = googleUserResult.Value;

        // Buscar si el usuario ya existe
        var existingUser = await dbContext.Users
        .AsSplitQuery()
            .Include(u => u.UserBranchRoles)
            .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RoleModulePermissions)
                    .ThenInclude(mp => mp.Module)
                        .ThenInclude(m => m.Menus)
            .FirstOrDefaultAsync(u => u.Email == googleUser!.Email);

        if (existingUser == null)
        {
            // Usuario nuevo - crear con rol Guest
            var createResult = await CreateGoogleUser(googleUser!);
            if (!createResult.IsSuccess)
                return createResult.Error!;

            existingUser = createResult.Value;
        }// else? maybe

        // Generar JWT token
        var token = tokenGenerator.GenerateAccessToken(existingUser!.Id);
        var refreshToken = tokenGenerator.GenerateRefreshToken();
        //BUILD PERMISSIONS
        var branchesResult = await UserMappingUtils.BuildBranchAccess(existingUser, branchService);
        if (!branchesResult.IsSuccess)
            return branchesResult.Error!;


        var response = new SuccessLoginDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            AuthProvider = existingUser.AuthProvider.ToString(),
            Status = existingUser.Status.ToString(),
            User = mapper.Map<UserDetailsDto>(existingUser),
            Branches = branchesResult.Value,
        };

        return response;

    }

    private async Task<Result<User>> CreateGoogleUser(GoogleUserInfo googleUser)
    {
        // Obtener rol Guest
        var roleResult = await registerUser.GetDefaultUserRole();
        if (!roleResult.IsSuccess)
            return roleResult.Error!;
        var user = new User
        {
            Email = googleUser.Email,
            Username = googleUser.Email.Split('@')[0], // Generar username del email
            FirstName = googleUser.GivenName,
            LastName = googleUser.FamilyName,
            // EmailVerified = googleUser.EmailVerified, // Google ya verificó el email
            Status = UserStatus.PendingRoleSelecting,
            AuthProvider = AuthProvider.Google,
            ExternalAuthId = googleUser.GoogleId,
            // ProfilePictureUrl = googleUser.Picture,
            UserBranchRoles = new List<UserBranchRole>
            {
                new UserBranchRole { RoleId = roleResult.Value }
            }
        };

        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        return user;
    }
        // private async Task UpdateGoogleInfo(User user, GoogleUserInfo googleUser)
        // {
        //     // Actualizar info si cambió
        //     bool hasChanges = false;

        //     if (user.ProfilePictureUrl != googleUser.Picture)
        //     {
        //         user.ProfilePictureUrl = googleUser.Picture;
        //         hasChanges = true;
        //     }

        //     if (!user.EmailVerified && googleUser.EmailVerified)
        //     {
        //         user.EmailVerified = true;
        //         hasChanges = true;
        //     }

        //     if (hasChanges)
        //     {
        //         await _dbContext.SaveChangesAsync();
        //     }
        // }

}
    
