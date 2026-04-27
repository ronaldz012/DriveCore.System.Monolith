using Auth.Contracts.Interfaces;
using Auth.UseCases.Autentication;
using Auth.UseCases.cache;
using Auth.UseCases.Features;
using Auth.UseCases.mapper;
using Auth.UseCases.Modules;
using Auth.UseCases.Roles;
using Auth.UseCases.Services;
using Auth.UseCases.Users;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Auth.UseCases;

public static class UseCasesDependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
        => services.AddFeatureUseCases()
                    .AddModulesUseCases()
                    .AddRolesUseCases()
                    .AddAutenticationUseCases()
                    .AddCache()
                    .AddUsersUseCases()
                    .AddMapper()
                    .AddServices()
                    .AddScoped<ICurrentUser, CurrentUserService>();
    static IServiceCollection AddServices(this IServiceCollection services)
    => services.AddScoped<IEmailVerificationService, EmailVerificationService>();

    static IServiceCollection AddUsersUseCases(this IServiceCollection services)
        => services.AddScoped<UserUserCases>()
            .AddScoped<GetAllUsers>()
            .AddScoped<CreateUser>();
    static IServiceCollection AddAutenticationUseCases(this IServiceCollection services)
    => services.AddScoped<AutenticationUseCases>()
                .AddScoped<RegisterUser>()
                .AddScoped<RegisterDefaultUser>()
                .AddScoped<Login>()
                .AddScoped<IAuthenticateMe, AutenticateMe>()
                .AddScoped<CompletePublicRegister>()
                .AddScoped<VerifyUser>()
                .AddScoped<AuthenticateWithGoogle>();

    static IServiceCollection AddCache(this IServiceCollection services)
    => services.AddScoped<IUserPermissionsCacheService, UserPermissionsCacheService>();
    static IServiceCollection AddFeatureUseCases(this IServiceCollection services)
    => services.AddScoped<FeatureUseCases>()
                .AddScoped<CreateFeature>()
                .AddScoped<GetFeature>()
                .AddScoped<ListFeatures>();
    
    
    static IServiceCollection AddModulesUseCases(this IServiceCollection services)
    => services.AddScoped<ModuleUseCases>()
        .AddScoped<CreateModuleUseCase>().AddScoped<ListModules>();
    // .AddScoped<UpdateModule>()
    // .AddScoped<DeleteModule>();

    static IServiceCollection AddRolesUseCases(this IServiceCollection services)
    => services.AddScoped<RoleUseCases>()
                .AddScoped<AddRole>()
                .AddScoped<GetRole>();
    static IServiceCollection AddMapper(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(MappingConfig).Assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
