using Auth.Data.Persistence;
using Auth.Dtos.Users;
using Auth.UseCases.Autentication.functions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Shared.Result;
using Shared.Services;

namespace Auth.UseCases.Autentication;

public class AutenticateMe(AuthDbContext context, ICurrentUser currentUser,  IMapper mapper)
{
    //sacar el token, leer el token y sacar id, devolver data no sensible (Reutilizar dtos )
    public async Task<Result<SuccessLoginDto?>> Execute()
    {
        var userId = currentUser.UserId;
        var user = await context.Users
            .AsSplitQuery()  
            .Include(u => u.UserRoles.Where(ur => ur.DeletedAt == null))
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RoleModulePermissions)
            .ThenInclude(rmp => rmp.Module)
            .ThenInclude(m => m.Menus)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        
   
        
        if (user == null) return new Error("user not found", "user not found");
        
        var roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct()
            .OrderBy(r => r)
            .ToList();
        
        var successLoginDto = new SuccessLoginDto
        {
            Status = user.Status.ToString(),
            AuthProvider = user.AuthProvider.ToString(),
            AccessToken = "",
            RefreshToken = "",
            ExpiresIn = 0,
            Roles = roles,
            Modules = UserMappingUtils.BuildUserModulesWithMenus(user),
            User = mapper.Map<UserDetailsDto>(user)
        };     
        
        return  successLoginDto;
    }
    
    
}