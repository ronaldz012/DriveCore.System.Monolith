using Auth.UseCases.Autentication;
using Auth.UseCases.Email;
using Auth.UseCases.mapper;
using Auth.UseCases.Menus;
using Auth.UseCases.Modules;
using Auth.UseCases.Roles;
using Auth.UseCases.Users;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Auth.UseCases;

public static class UseCasesDependencyInjection
{
    public static IServiceCollection AddUseCases(this IServiceCollection services)
        => services.AddMenuUseCases()
                    .AddModulesUseCases()
                    .AddRolesUseCases()
                    .AddAutenticationUseCases()
                    .AddUsersUseCases()
                   .AddMapper()
                   .AddServices();
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
                .AddScoped<AutenticateMe>()
                .AddScoped<CompletePublicRegister>()
                .AddScoped<VerifyUser>()
                .AddScoped<AuthenticateWithGoogle>();
    static IServiceCollection AddMenuUseCases(this IServiceCollection services)
    => services.AddScoped<MenuUseCases>()
                .AddScoped<AddMenu>()
                .AddScoped<GetMenu>()
                .AddScoped<GetAllMenus>()
                .AddScoped<UpdateMenu>()
                .AddScoped<DeleteMenu>();

    static IServiceCollection AddModulesUseCases(this IServiceCollection services)
    => services.AddScoped<ModulesUseCases>()
                .AddScoped<AddModule>()
                .AddScoped<GetModule>()
                .AddScoped<GetAllModules>();
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
