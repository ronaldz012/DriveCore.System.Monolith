using Branches.Contracts;
using Branches.module.Services;
using Microsoft.Extensions.Configuration;

namespace Branches.module;
using Microsoft.Extensions.DependencyInjection;

public static  class BranchDI
{
    public static IServiceCollection AddBranch(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IBranchService, BranchService>();
        return services;        
    }
}