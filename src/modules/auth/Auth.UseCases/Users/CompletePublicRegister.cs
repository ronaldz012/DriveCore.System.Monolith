using System;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Dtos.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

namespace Auth.UseCases.Users;

public class CompletePublicRegister(AuthDbContext dbContext )
{
    public async Task<Result<bool>> Execute(CompleteUserRoleDto dto, int userId) // update with I Currente User Service
    {
        User? user = await dbContext.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return new Error("NOT_FOUND", "User not found");

        if (user.Status == UserStatus.PendingVerification)
            return new Error("VALIDATION_ERROR", "verified email");

        var roleId = await dbContext.Roles
                .Where(r => r.Public && r.Name == dto.RoleType)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

        if (roleId <= 0)
            return new Error("NOT_FOUND", "role not found");

        user.UserRoles.Clear();
        user.UserRoles.Add(new UserRole { RoleId = roleId, UserId = userId });  //test id UserId implicit is neccesary

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        //other properties, use mapper if it get complex

        await dbContext.SaveChangesAsync();
        return true;
    }
}
