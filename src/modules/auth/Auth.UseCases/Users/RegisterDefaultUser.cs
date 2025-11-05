using System;
using Auth.Data.Entities;
using Auth.Dtos.Users;
using MapsterMapper;
using Microsoft.VisualBasic;
using Shared.Result;

namespace Auth.UseCases.Users;

public class RegisterDefaultUser(IMapper mapper, RegisterUser registerUser)
{
    public async Task<Result<bool>> Execute(RegisterUserDto dto)
    {
         var roleResult = await registerUser.GetDefaultUserRole();
        if (!roleResult.IsSuccess)
            return roleResult.Error!;
        var user = mapper.Map<User>(dto);
        user.Status = UserStatus.PendingVerification; //this should be based on the settings or always be this way? not sure
        user.UserRoles = new List<UserRole>
        {
            new UserRole { RoleId = roleResult.Value }
        };

        return await registerUser.Execute(user, dto.Password);
    }

}
