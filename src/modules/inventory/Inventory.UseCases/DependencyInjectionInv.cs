using Inventory.UseCases.Brands;
using Inventory.UseCases.Categories;
using Inventory.UseCases.ProductUseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.UseCases;

public  static class DependencyInjectionInv
{
    public static IServiceCollection AddInventory(this IServiceCollection services)
    {
        services.AddScoped<InvUseCases>()
            .AddScoped<CreateProduct>()
            .AddScoped<GetCategories>();
        
        services.AddScoped<CategoryUseCases>()
            .AddScoped<CreateCategory>();
        
        services.AddScoped<BrandUseCases>()
            .AddScoped<CreateBrand>();
        return services;
    }
}