using Auth.Contracts.Dtos.Users;
using Auth.Contracts.Interfaces;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Result;
using Shared.Services;

namespace Auth.UseCases.Autentication;

public class CompletePublicRegister(AuthDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<Result<bool>> Execute(CompleteUserRoleDto dto) // update with I Currente User Service
    {
        User? user = await dbContext.Users.Include(u => u.UserBranchRoles).FirstOrDefaultAsync(u => u.Id == currentUser.UserId);
        if (user == null)
            return new Error("NOT_FOUND", "User not found");

        if (user.Status == UserStatus.PendingVerification)
            return new Error("VALIDATION_ERROR", "verified email");
        if(user.Status != UserStatus.PendingRoleSelecting)
            return new Error("VALIDATION_ERROR", "role not pending");
        

        var roleId = await dbContext.Roles
                .Where(r => r.Public && r.Name == dto.RoleType)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

        if (roleId <= 0)
            return new Error("NOT_FOUND", "role not found");

        user.UserBranchRoles.Clear();
        user.UserBranchRoles.Add(new UserBranchRole { RoleId = roleId, UserId = currentUser.UserId });  //test id UserId implicit is neccesary

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        //other properties, use mapper if it get complex
        user.Status = UserStatus.Active;

        await dbContext.SaveChangesAsync();
        return true;
    }
}
