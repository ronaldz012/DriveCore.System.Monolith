using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Services;

namespace Shared;

public static class SharedDependencyInjection
{
    public static IServiceCollection AddShared(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        IConfigurationSection tokenSettingsSection = configuration.GetSection(SmtpSettings.SectionName);
        services.Configure<SmtpSettings>(tokenSettingsSection);
        return services;        
    }
}
