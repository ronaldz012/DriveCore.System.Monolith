using System;
using System.Runtime.Loader;
using Auth.Data.Entities;
using Auth.Data.Migrations;
using Auth.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1;
using Shared.Result;

namespace Auth.UseCases.Users;

public class VerifyUser(AuthDbContext dbContext )
{
    public async Task<Result<bool>> Execute(string verifyCode)
    {
        EmailVerificationCode? code = dbContext.EmailVerificationCodes.Include(u => u.User).FirstOrDefault(c => c.Code == verifyCode
                                                                                            && !c.IsUsed
                                                                                            && c.Purpose == VerificationCodePurpose.AccountVerification);
        if (code == null)
            return new Error("NOT_FOUND", "Verification Code not found");
        if (code.ExpiresAt > DateTime.UtcNow)
            return new Error("INVALID_OPERATION", "Verification code Expired");
        code.IsUsed = true;
        code.User.Status = UserStatus.Active;
        await dbContext.SaveChangesAsync();
        return true;
    } 
}
