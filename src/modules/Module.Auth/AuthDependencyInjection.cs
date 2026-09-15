using Common.Contracts.authentication;
using Common.Contracts.branches;
using Common.Contracts.Seeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.Auth.Application.Abstraction;
using Module.Auth.Application.UseCases.Branches;
using Module.Auth.Application.UseCases.Branches.CreateBranch;
using Module.Auth.Application.UseCases.Branches.GetBranches;
using Module.Auth.Application.UseCases.Branches.UpdateBranch;
using Module.Auth.Application.UseCases.Branches.ToggleBranchStatus;
using Module.Auth.Application.UseCases.Branches.GetBranchDetails;
using Module.Auth.Application.UseCases.Branches.GetBranchTypes;
using Module.Auth.Application.UseCases.Branches.SetBranchCompleteness;
using Module.Auth.Application.UseCases.Features;
using Module.Auth.Application.UseCases.Roles;
using Module.Auth.Application.UseCases.Tenant;
using Module.Auth.Application.UseCases.Tenant.Create;
using Module.Auth.Application.UseCases.TenantDatabases;
using Module.Auth.Application.UseCases.TenantDatabases.Get;
using Module.Auth.Application.UseCases.TenantDatabases.GetById;
using Module.Auth.Application.UseCases.Users;
using Module.Auth.Application.UseCases.Users.CreateUser;
using Module.Auth.Application.UseCases.Users.CreateTenantAdmin;
using Module.Auth.Application.UseCases.Users.GetAllUsers;
using Module.Auth.Application.UseCases.Users.UpdateUserStatus;
using Module.Auth.Application.UseCases.Users.UpdateUser;
using Module.Auth.Application.UseCases.Users.GetUserDetails;
using Module.Auth.Application.UseCases.Users.ToggleUserType;
using Module.Auth.Infrastructure.Authentication;
using Module.Auth.Infrastructure.Branches;
using Module.Auth.Infrastructure.Databases;
using Module.Auth.Infrastructure.Persistence;
using Module.Auth.Infrastructure.Seeder;
using Module.Auth.Application.UseCases.Roles.GetById;
using Module.Auth.Application.UseCases.Roles.Create;
using Module.Auth.Application.UseCases.Roles.GetRoles;

namespace Module.Auth;

public static class SharedDependencyInjection
{
    public static IServiceCollection AuthDependencyInjection (this IServiceCollection services, IConfiguration configuration)
    {
        
         services.AddScoped<FeatureUseCases>()
            .AddScoped<CreateFeature>()
            .AddScoped<GetFeature>()
            .AddScoped<ListFeatures>();
         

         services.AddScoped<BranchesUseCases>()
             .AddScoped<CreateBranch>()
             .AddScoped<GetBranches>()
             .AddScoped<UpdateBranch>()
               .AddScoped<ToggleBranchStatus>()
               .AddScoped<GetBranchDetails>()
               .AddScoped<GetBranchTypes>()
               .AddScoped<SetBranchCompleteness>();

         services.AddScoped<RoleUseCases>()
             .AddScoped<GetRoleById>()
             .AddScoped<AddRole>()
             .AddScoped<GetRoles>();
         
        
         
         services.AddScoped<UserUserCases>()
                 .AddScoped<GetAllUsers>()
                 .AddScoped<CreateUser>()
                 .AddScoped<CreateTenantAdmin>()
                 .AddScoped<UpdateUserStatus>()
                 .AddScoped<UpdateUser>()
                 .AddScoped<GetUserDetails>()
                 .AddScoped<ToggleUserType>();

         services.AddScoped<TenantDatabaseUseCases>()
             .AddScoped<GetTenantDatabases>()
             .AddScoped<GetTenantDatabaseDetails>();

         services.AddScoped<TenantUseCases>()
             .AddScoped<CreateTenant>() ;            

         

         IConfigurationSection authSettingsSection = configuration.GetSection(AuthenticationSettings.SectionName);
         services.Configure<AuthenticationSettings>(authSettingsSection);

         services.AddOptions<Auth0Settings>()
             .Bind(configuration.GetSection(Auth0Settings.SectionName))
             .Validate(s => !string.IsNullOrWhiteSpace(s.Domain), "Auth0:Domain is required")
             .Validate(s => !string.IsNullOrWhiteSpace(s.Audience), "Auth0:Audience is required")
             .Validate(s => !string.IsNullOrWhiteSpace(s.SpaClientSecret), "Auth0:SpaClientSecret is required for JWE decryption")
             .Validate(s => !string.IsNullOrWhiteSpace(s.Connection), "Auth0:Connection is required")
             .Validate(s => !string.IsNullOrWhiteSpace(s.M2M.StaticAccessToken) || (!string.IsNullOrWhiteSpace(s.M2M.ClientId) && !string.IsNullOrWhiteSpace(s.M2M.ClientSecret)), "Auth0:M2M:ClientId and ClientSecret are required when StaticAccessToken is empty")
             .ValidateOnStart();

         IConfigurationSection projectInfoSection = configuration.GetSection(ProjectInfo.SectionName);
         services.Configure<ProjectInfo>(projectInfoSection);

         services.AddHttpClient<IAuth0ProvisioningService, Auth0ProvisioningService>(client =>
         {
             client.Timeout = TimeSpan.FromSeconds(30);
         });

         services.AddScoped<IAuthDbContext>(sp =>
             sp.GetRequiredService<AuthDbContext>());
         // Infrastructure service registrations
         services.AddScoped<IBranchService, BranchService>();
         //INTEGRATION
          services.AddScoped<IUserIntegrationService, UserIntegrationService>();
          services.AddScoped<ISessionStateService, SessionStateService>();

         services.AddScoped<IDbConnectionTester, DbConnectionTester>();
         services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolverService>();
         
         services.AddScoped<IDataSeeder, TenantDataBaseSeeder>()
             .AddScoped<IDataSeeder, FeatureSeeder>()
             .AddScoped<IDataSeeder, PlanSeeder>()
             .AddScoped<IDataSeeder, TenantSeeder>()
             .AddScoped<IDataSeeder, Auth0Seeder>();

        services.AddScoped<ITenantConnectionContext, TenantConnectionContext>();

        services.AddDbContext<AuthDbContext>((sp, options) =>
        {
            var connection = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connection,
                x => x.MigrationsHistoryTable("__EFMigrationsHistory_shared", null));
        });

         return services;
    }
    
}
