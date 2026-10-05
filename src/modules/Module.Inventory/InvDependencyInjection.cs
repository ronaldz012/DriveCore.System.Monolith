using Common.Contracts.inventory;
using Common.Contracts.Seeder;
using Microsoft.Extensions.DependencyInjection;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Infrastructure.Services;
using Module.Inventory.Application.UseCases.Brands;
using Module.Inventory.Application.UseCases.Brands.CreateBrand;
using Module.Inventory.Application.UseCases.Brands.GetBrands;
using Module.Inventory.Application.UseCases.Brands.Update;
using Module.Inventory.Application.UseCases.Categories;
using Module.Inventory.Application.UseCases.Categories.Create;
using Module.Inventory.Application.UseCases.Categories.Get;
using Module.Inventory.Application.UseCases.Categories.Update;
using Module.Inventory.Application.UseCases.Colors;
using Module.Inventory.Application.UseCases.Colors.Create;
using Module.Inventory.Application.UseCases.Colors.List;
using Module.Inventory.Application.UseCases.Colors.Update;
using Module.Inventory.Application.UseCases.Sizes;
using Module.Inventory.Application.UseCases.Sizes.Create;
using Module.Inventory.Application.UseCases.Sizes.List;
using Module.Inventory.Application.UseCases.Sizes.Update;
using Module.Inventory.Application.UseCases.Products;
using Module.Inventory.Application.UseCases.Products.Create;
using Module.Inventory.Application.UseCases.Products.Delete;
using Module.Inventory.Application.UseCases.Products.Get;
using Module.Inventory.Application.UseCases.Products.GetById;
using Module.Inventory.Application.UseCases.Products.Search;
using Module.Inventory.Application.UseCases.Products.Update;
using Module.Inventory.Application.UseCases.Products.UpdateStatus;
using Module.Inventory.Application.UseCases.ProductVariants;
using Module.Inventory.Application.UseCases.ProductVariants.Create;
using Module.Inventory.Application.UseCases.ProductVariants.Delete;
using Module.Inventory.Application.UseCases.ProductVariants.GetById;
using Module.Inventory.Application.UseCases.ProductVariants.GetBySku;
using Module.Inventory.Application.UseCases.ProductVariants.PatchStock;
using Module.Inventory.Application.UseCases.ProductVariants.Search;
using Module.Inventory.Application.UseCases.ProductVariants.Update;
using Module.Inventory.Application.UseCases.Providers;
using Module.Inventory.Application.UseCases.Providers.CreateProvider;
using Module.Inventory.Application.UseCases.Providers.GetProviders;
using Module.Inventory.Application.UseCases.Providers.Update;
using Module.Inventory.Application.UseCases.Receptions;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Application.UseCases.Receptions.Get;
using Module.Inventory.Application.UseCases.Receptions.GetById;
using Module.Inventory.Application.UseCases.Receptions.GetLabels;
using Module.Inventory.Application.UseCases.Receptions.Revert;
using Module.Inventory.Application.UseCases.Transfers;
using Module.Inventory.Application.UseCases.Transfers.Cancel;
using Module.Inventory.Application.UseCases.Transfers.Create;
using Module.Inventory.Application.UseCases.Transfers.Get;
using Module.Inventory.Application.UseCases.Transfers.GetById;
using Module.Inventory.Application.UseCases.Transfers.Resolve;
using Module.Inventory.Application.UseCases.StockMovements;
using Module.Inventory.Application.UseCases.StockMovements.Get;
using Module.Inventory.Infrastructure;
using Module.Inventory.Infrastructure.Seeder;

namespace Module.Inventory;

public  static class InvDependencyInjection
{
    public static IServiceCollection AddInventory(this IServiceCollection services)
    {
        services.AddScoped<ProductUseCases>()
            .AddScoped<CreateProductUc>()
            .AddScoped<GetProductsUc>()
            .AddScoped<SearchProduct>()
            .AddScoped<ProductDetails>()
            .AddScoped<GetProductVariantByCode>()
            .AddScoped<SearchProductVariants>()
            .AddScoped<UpdateProduct>()
            .AddScoped<DeleteProduct>()
            .AddScoped<UpdateProductStatus>();

        services.AddScoped<ProductVariantUseCases>()
            .AddScoped<GetProductVariantDetails>()
            .AddScoped<GetProductVariantDetails>()
            .AddScoped<UpdateProductVariant>()
            .AddScoped<CorrectProductVariantStock>()
            .AddScoped<CreateProductVariantUc>()
            .AddScoped<DeleteProductVariantUc>();

      
        
        services.AddScoped<CategoryUseCases>()
            .AddScoped<CreateCategory>()
            .AddScoped<GetCategories>()
            .AddScoped<UpdateCategory>();
        
        services.AddScoped<BrandUseCases>()
            .AddScoped<CreateBrandUc>()
            .AddScoped<GetBrands>()
            .AddScoped<UpdateBrand>();
        
        services.AddScoped<ReceptionUseCases>()
            .AddScoped<CreateReceptionUc>()
            .AddScoped<ListReceptions>()
            .AddScoped<GetReception>()
            .AddScoped<ReceptionLabels>()
            .AddScoped<RevertStockReception>();

        services.AddScoped<StockTransferUseCases>()
            .AddScoped<CreateStockTransfer>()
            .AddScoped<ResolveStockTransfer>()
            .AddScoped<StockTransferDetails>()
            .AddScoped<CancelStockTransfer>()
            .AddScoped<ListStockTransfers>();

        services.AddScoped<StockMovementUseCases>()
            .AddScoped<ListStockMovements>();

        services.AddScoped<ColoreUseCases>()
            .AddScoped<CreateColor>()
            .AddScoped<GetListColors>()
            .AddScoped<UpdateColor>();

        services.AddScoped<SizeUseCases>()
            .AddScoped<CreateSize>()
            .AddScoped<GetListSizes>()
            .AddScoped<UpdateSize>();

        services.AddScoped<ProviderUseCases>()
            .AddScoped<CreateProviderUc>()
            .AddScoped<GetProviders>()
            .AddScoped<UpdateProvider>();

        services.AddScoped<IInventoryIntegrationService, InventoryIntegrationService>();
        services.AddScoped<IProductCodeService, ProductCodeService>();
        services.AddScoped<IInventoryNumberGenerator, InventoryNumberGenerator>();
        services.AddScoped<IDefaultCatalogProvisioner, DefaultCatalogProvisioner>();

        services.AddScoped<IDataSeeder, InventorySeeder>();
        services.AddScoped<IDataSeeder, DefaultCatalogSeeder>();
        services.AddScoped<IDataSeeder, StockReceptionSeeder>();
        services.AddScoped<IDataSeeder, StockTransferSeeder>();

        return services;
    }
}