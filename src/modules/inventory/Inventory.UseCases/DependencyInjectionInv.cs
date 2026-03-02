using Inventory.UseCases.Categories;
using Inventory.UseCases.ProductUseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.UseCases;

public  static class DependencyInjectionInv
{
    public static IServiceCollection AddInventory(this IServiceCollection services)
    {
        services.AddScoped<InvUseCases>()
            .AddScoped<CreateProduct>();
        
        services.AddScoped<CategoryUseCases>()
            .AddScoped<CreateCategory>();
        return services;
    }
}