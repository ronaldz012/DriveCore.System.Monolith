using Inventory.UseCases.Brands;
using Inventory.UseCases.Categories;
using Inventory.UseCases.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.UseCases;

public  static class DependencyInjectionInv
{
    public static IServiceCollection AddInventory(this IServiceCollection services)
    {
        services.AddScoped<ProductUseCases>()
            .AddScoped<CreateProduct>()
            .AddScoped<GetProducts>();
        
        services.AddScoped<CategoryUseCases>()
            .AddScoped<CreateCategory>()
            .AddScoped<GetCategories>();
        
        services.AddScoped<BrandUseCases>()
            .AddScoped<CreateBrand>()
            .AddScoped<GetBrands>();
        return services;
    }
}