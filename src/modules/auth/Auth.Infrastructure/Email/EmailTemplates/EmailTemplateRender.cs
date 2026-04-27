using Auth.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Email.EmailTemplates;

public class EmailTemplateRenderer(IOptions<ProjectInfo> projectInfo,
    IOptions<AuthenticationSettings> authenticationSettings) 
{
    private readonly AppBranding _appBranding = projectInfo.Value.AppBranding;
    private readonly EmailTemplateDefaults _templateDefaults = projectInfo.Value.EmailTemplateDefaults;
    private readonly EmailVerificationSettings _emailVerificationSettings = authenticationSettings.Value.EmailVerification;

    private async Task<string> GetBaseHtmlContentAsync(string templateFileName)
    {

        var templatePath = Path.Combine(AppContext.BaseDirectory,"Email","EmailTemplates", templateFileName);
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Email template not found at: {templatePath}. Ensure '{templateFileName}' is copied to output directory.");
        }

        string htmlContent = await File.ReadAllTextAsync(templatePath);

        var commonReplacements = new Dictionary<string, string>
            {
                { "##PROJECT_NAME##", _appBranding.AppName },
                { "##PROJECT_LOGO_URL##", _appBranding.AppLogoUrl },
                { "##PROJECT_PRIMARY_COLOR##", _appBranding.AppPrimaryColor },
                { "##PROJECT_SUPPORT_EMAIL##", _appBranding.SupportEmail },
                { "##PROJECT_ADDRESS##", _appBranding.Address },
                { "##CURRENT_YEAR##", DateTime.UtcNow.Year.ToString() },
                { "##FOOTER_DISCLAIMER##", _templateDefaults.DefaultFooterText }
            };

        foreach (var replacement in commonReplacements)
        {
            htmlContent = htmlContent.Replace(replacement.Key, replacement.Value);
        }
        return htmlContent;
    }


    public async Task<string> GetAccountVerificationEmailBody(
        string userName,
        string verificationCode,
        DateTime expiresAt)
    {
        string htmlContent = await GetBaseHtmlContentAsync("EmailVerification.html");

        // Reemplazos específicos para la verificación de cuenta
        var accountVerificationReplacements = new Dictionary<string, string>
            {
                { "##EMAIL_SUBJECT##", $"Verifica tu Cuenta de {_appBranding.AppName}" },
                { "##EMAIL_MAIN_HEADING##", $"¡Bienvenido a {_appBranding.AppName}!" },
                { "##USER_NAME##", userName }, 
                { "##EMAIL_BODY_CONTENT##", $"Gracias por registrarte en {_appBranding.AppName}. Por favor, usa el siguiente código para verificar tu cuenta:" },
                { "##DYNAMIC_CODE##", verificationCode },
                { "##EXPIRATION_TIME##", _emailVerificationSettings.TokenExpirationHours.ToString() } // REVISAR
            };

        foreach (var replacement in accountVerificationReplacements)
        {
            htmlContent = htmlContent.Replace(replacement.Key, replacement.Value);
        }

        return htmlContent;
    }

    public async Task<string> GetPasswordResetEmailBody(
        string userName,
        string verificationCode,
        DateTime expiresAt)
    {
        string htmlContent = await GetBaseHtmlContentAsync("EmailVerification.html"); // Podrías usar "PasswordReset.html" si tienes una plantilla específica

        var passwordResetReplacements = new Dictionary<string, string>
            {
                { "##EMAIL_SUBJECT##", $"Restablece tu Contraseña - {_appBranding.AppName}" },
                { "##EMAIL_MAIN_HEADING##", $"Restablece tu Contraseña" },
                { "##USER_NAME##", userName },
                { "##EMAIL_BODY_CONTENT##", $"Has solicitado restablecer tu contraseña para {_appBranding.AppName}. Usa el siguiente código para proceder:" },
                { "##DYNAMIC_CODE##", verificationCode },
                { "##EXPIRATION_TIME##", _emailVerificationSettings.TokenExpirationHours.ToString() }
            };

        foreach (var replacement in passwordResetReplacements)
        {
            htmlContent = htmlContent.Replace(replacement.Key, replacement.Value);
        }

        return htmlContent;
    }

    public async Task<string> GetEmailChangeConfirmationBody(
        string userName,
        string verificationCode,
        DateTime expiresAt)
    {
        string htmlContent = await GetBaseHtmlContentAsync("EmailVerification.html"); // Podrías usar "EmailChange.html" si tienes una plantilla específica

        var emailChangeReplacements = new Dictionary<string, string>
            {
                { "##EMAIL_SUBJECT##", $"Confirma tu cambio de Email - {_appBranding.AppName}" },
                { "##EMAIL_MAIN_HEADING##", $"Confirma tu Cambio de Email" },
                { "##USER_NAME##", userName },
                { "##EMAIL_BODY_CONTENT##", $"Has solicitado cambiar tu dirección de correo electrónico en {_appBranding.AppName}. Usa el siguiente código para confirmar este cambio:" },
                { "##DYNAMIC_CODE##", verificationCode },
                { "##EXPIRATION_TIME##", _emailVerificationSettings.TokenExpirationHours.ToString() }
            };

        foreach (var replacement in emailChangeReplacements)
        {
            htmlContent = htmlContent.Replace(replacement.Key, replacement.Value);
        }

        return htmlContent;
    }
}