using System;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Dtos.Modules;
using Auth.Dtos.Users;
using Auth.Infrastructure.Authentication;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql.TypeMapping;
using Shared.Result;

namespace Auth.UseCases.Users;

public class Login(AuthDbContext dbContext, ITokenGenerator tokenGenerator, IMapper mapper)
{
    public async Task<Result<SuccesLoginDto>> Execute(LoginDto request)
    {
        var user = await dbContext.Users
            .AsSplitQuery()  
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

        if (!ValidatePassword.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return new Error("VALIDATION_ERROR", "Correo electrónico o contraseña incorrectos.");
        }
        if(user.Status == UserStatus.PendingVerification)
        {
            return new SuccesLoginDto
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

        var modules = BuildUserModulesWithMenus(user);

        var succesLoginDto = new SuccesLoginDto
        {
            Status = user.Status.ToString(),
            AuthProvider = user.AuthProvider.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = tokenGenerator.GetAccessTokenExpirationMinutes() * 60,
            Roles = roles,
            Modules = modules,
            User = mapper.Map<UserDetailsDto>(user)
        };

        return succesLoginDto;
    }

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