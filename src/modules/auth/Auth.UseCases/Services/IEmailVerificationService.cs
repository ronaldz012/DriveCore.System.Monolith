using System.Security.Cryptography;
using System.Text;
using Auth.Data.Entities;
using Auth.Data.Persistence;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication;
using Auth.Infrastructure.Email.EmailTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Services;

namespace Auth.UseCases.Services;

public interface IEmailVerificationService
{
    Task SendVerificationEmailAsync(User user, VerificationCodePurpose purpose);
    Task<bool> ValidateVerificationCodeAsync(int userId, string code, VerificationCodePurpose purpose);
    Task ResendVerificationEmailAsync(int userId, string userEmail, VerificationCodePurpose purpose);
}

public class EmailVerificationService(AuthDbContext dbContext,
                                    IOptions<AuthenticationSettings> authenticationSettings,
                                    IOptions<ProjectInfo> projectInfo,
                                    IEmailService emailService,
                                    EmailTemplateRenderer emailTemplateRenderer) : IEmailVerificationService
{
    private readonly EmailVerificationSettings _emailVerificationSettings = authenticationSettings.Value.EmailVerification;
    private readonly AppBranding _appBranding = projectInfo.Value.AppBranding;
    public async Task SendVerificationEmailAsync(User user, VerificationCodePurpose purpose)
    {
        await InvalidateExistingCodesAsync(user.Id, purpose);

        string verificationCode = GenerateVerificationCode(_emailVerificationSettings.VerificationCodeLength);
            DateTime sentAt = DateTime.UtcNow;
            DateTime expiresAt = sentAt.AddHours(_emailVerificationSettings.TokenExpirationHours);
        
        var newVerificationCode = new EmailVerificationCode
            {
                UserId = user.Id,
                Code = verificationCode,
                Email = user.Email,
                SentAt = sentAt,
                ExpiresAt = expiresAt,
                IsUsed = false,
                Attempts = 0,
                Purpose = purpose
            };

            await dbContext.AddAsync(newVerificationCode);
            await dbContext.SaveChangesAsync();
        
            string userName = user.FirstName + user.LastName; 

            string emailSubject;
            string emailBody;

            switch (purpose)
            {
                case VerificationCodePurpose.AccountVerification:
                    emailSubject = $"Verifica tu cuenta - {_appBranding.AppName}";
                    // Call the correct async method from EmailTemplateRenderer
                    emailBody = await emailTemplateRenderer.GetAccountVerificationEmailBody(
                                        userName, verificationCode, expiresAt);
                    break;
                case VerificationCodePurpose.PasswordReset:
                    emailSubject = $"Restablece tu contraseña - {_appBranding.AppName}";
                    // Call the correct async method from EmailTemplateRenderer
                    emailBody = await emailTemplateRenderer.GetPasswordResetEmailBody(
                                        userName, verificationCode, expiresAt);
                    break;
                case VerificationCodePurpose.EmailChange:
                    emailSubject = $"Confirma tu cambio de email - {_appBranding.AppName}";
                    // Call the correct async method from EmailTemplateRenderer
                    emailBody = await emailTemplateRenderer.GetEmailChangeConfirmationBody(
                                        userName, verificationCode, expiresAt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(purpose), $"Unsupported verification purpose: {purpose}");
            }

            // 6. Send the email using your EmailSenderService
            await emailService.SendEmailAsync(user.Email, emailSubject, emailBody, isHtml: true);

    }
    public Task ResendVerificationEmailAsync(int userId, string userEmail, VerificationCodePurpose purpose)
    {
        throw new NotImplementedException();
    }

    

    public Task<bool> ValidateVerificationCodeAsync(int userId, string code, VerificationCodePurpose purpose)
    {
        throw new NotImplementedException();
    }

    private string GenerateVerificationCode(int length)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var result = new StringBuilder(length);
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] data = new byte[length];
            rng.GetBytes(data);
            foreach (byte b in data)
            {
                result.Append(chars[b % chars.Length]);
            }
        }
        return result.ToString();
    }
    private async Task InvalidateExistingCodesAsync(int userId, VerificationCodePurpose purpose)
    {
        var existingCodes = await dbContext.EmailVerificationCodes.Where(
            c => c.UserId == userId &&
                 c.Purpose == purpose &&
                 !c.IsUsed &&
                 c.ExpiresAt > DateTime.UtcNow).ToListAsync();
        bool changesMade = false;
        foreach (var code in existingCodes)
        {
            code.IsUsed = true; // Mark as logically used/invalidated
            dbContext.Update(code);
            changesMade = true;
        }
        if (changesMade)
        {
            await dbContext.SaveChangesAsync(); // Save changes if any codes were invalidated
        }
    }

}
