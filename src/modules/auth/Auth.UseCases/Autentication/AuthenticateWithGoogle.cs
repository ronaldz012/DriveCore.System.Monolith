using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Dtos.Modules;
using Auth.Dtos.Users;
using Auth.Infrastructure.Authentication;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Autentication;

public class AuthenticateWithGoogle(AuthDbContext dbContext,  RegisterUser registerUser, IMapper mapper, IGoogleTokenValidator googleTokenValidator, ITokenGenerator tokenGenerator)
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
            .Include(u => u.UserRoles)
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
        var roles = existingUser.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct()
            .OrderBy(r => r)
            .ToList();


        var response = new SuccessLoginDto
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            AuthProvider = existingUser.AuthProvider.ToString(),
            Status = existingUser.Status.ToString(),
            User = mapper.Map<UserDetailsDto>(existingUser),
            Roles = roles,
            Modules = BuildUserModulesWithMenus(existingUser)
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
            UserRoles = new List<UserRole>
            {
                new UserRole { RoleId = roleResult.Value }
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
    private List<ModulePermissionsDeductedDto> BuildUserModulesWithMenus(User user)
    {
        // UNION DE PERMISOS
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
            //.Where(x => x.CanRead)  // Solo módulos con al menos lectura
            //.OrderBy(x => x.Module.Order)
            .ToList();

        // Construir DTOs
        var moduleDtos = modulePermissions.Select(mp => new ModulePermissionsDeductedDto
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

        return moduleDtos;
    }
}
    
