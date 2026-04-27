using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Infrastructure.Authentication;
using Auth.UseCases.Autentication.functions;
using Auth.UseCases.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Shared.Result;

namespace Auth.UseCases.Autentication;

public class RegisterUser(AuthDbContext dbContext,IEmailVerificationService emailVerificationService, IOptions<AuthenticationSettings> authSettings, IConfiguration config)
{
    private readonly IOptions<AuthenticationSettings> _authSettings = authSettings;
    public async Task<Result<bool>> Execute(User user, string password)
    {
        var emailAndUserNameAvailable = await dbContext.Users
        .AnyAsync(u => u.Email == user.Email || u.Username == user.Username);
        if (emailAndUserNameAvailable)
        {
            return new Error("DUPLICATE", "El correo electrónico o el nombre de usuario ya están en uso.");
        }
        var emailVerificationRequired = _authSettings.Value.EmailVerification.Required &&
            _authSettings.Value.EmailVerification.RequiredForProviders.Contains("Local");


        var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var userToCreate = user;
            byte[] passwordHash, passwordSalt;
            ValidatePassword.CreatePasswordHash(password, out passwordHash, out passwordSalt);
            userToCreate.PasswordHash = passwordHash;
            userToCreate.PasswordSalt = passwordSalt;
            await dbContext.Users.AddAsync(userToCreate);

            if (emailVerificationRequired)
            {
                await dbContext.SaveChangesAsync(); // To Get the Id of the user
                await emailVerificationService.SendVerificationEmailAsync(userToCreate, VerificationCodePurpose.AccountVerification);
            }
            else
            {
                userToCreate.Status = UserStatus.Active;
                await dbContext.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result<int>> GetDefaultUserRole()
    {
        var roleName = config["Roles:DefaultUserRole"] ?? "User";
        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
            return new Error("NOT_FOUND", "El rol por defecto no está configurado en el sistema.");

        return role.Id;
    } 
    




    
}
