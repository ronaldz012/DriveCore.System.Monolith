using System;
using Auth.Infrastructure.Authentication;
using Auth.Infrastructure.Email.Emailtemplates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection tokenSettingsSection = configuration.GetSection(TokenSettings.SectionName);
        services.Configure<TokenSettings>(tokenSettingsSection);

        IConfigurationSection authSettingsSection = configuration.GetSection(AuthenticationSettings.SectionName);
        services.Configure<AuthenticationSettings>(authSettingsSection);

        IConfigurationSection smtpSettingsSection = configuration.GetSection(Email.SmtpSettings.SectionName);
        services.Configure<Email.SmtpSettings>(smtpSettingsSection);

        IConfigurationSection projectInfoSection = configuration.GetSection(ProjectInfo.SectionName);
        services.Configure<ProjectInfo>(projectInfoSection);

        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        services.AddSingleton<EmailTemplateRenderer>();
        return services;
        
    }
       
}
