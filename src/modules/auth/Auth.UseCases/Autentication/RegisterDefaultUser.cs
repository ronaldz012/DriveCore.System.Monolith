using Auth.Contracts.Dtos.Users;
using Auth.Data.Entities;
using MapsterMapper;
using Shared.Result;

namespace Auth.UseCases.Autentication;

public class RegisterDefaultUser(IMapper mapper, RegisterUser registerUser)
{
    public async Task<Result<bool>> Execute(RegisterUserDto dto)
    {
         var roleResult = await registerUser.GetDefaultUserRole();
        if (!roleResult.IsSuccess)
            return roleResult.Error!;
        var user = mapper.Map<User>(dto);
        user.Status = UserStatus.PendingVerification; //this should be based on the settings or always be this way? not sure
        user.UserBranchRoles = new List<UserBranchRole>
        {
            new UserBranchRole { RoleId = roleResult.Value }
        };

        return await registerUser.Execute(user, dto.Password);
    }

}
