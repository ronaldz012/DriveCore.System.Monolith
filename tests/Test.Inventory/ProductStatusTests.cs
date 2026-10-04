using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Module.Inventory.Application.UseCases.ProductVariants.GetBySku;
using Module.Inventory.Application.UseCases.Products.Get;
using Module.Inventory.Application.UseCases.Products.GetById;
using Module.Inventory.Application.UseCases.Products.Search;
using Module.Inventory.Application.UseCases.Products.UpdateStatus;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Organization;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Receptions;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class ProductStatusTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid BrandId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid ColorId = Guid.NewGuid();
    private static readonly Guid SizeId = Guid.NewGuid();
    private static readonly Guid ProviderId = Guid.NewGuid();

    private static TestAppDbContext CreateDbContext()
    {
        var tenantCtx = new TestTenantConnectionContext
        {
            TenantId = TenantId,
            Schema = "test_schema",
            DatabaseName = "test_db"
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"InvTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestAppDbContext(options, tenantCtx);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static void SeedCatalog(TestAppDbContext ctx)
    {
        ctx.Brands.Add(new Brand { Id = BrandId, Name = "Nike", Prefix = "NIK", CreatedBy = TenantId, CreatedByName = "Test User" });
        ctx.Categories.Add(new Category { Id = CategoryId, Name = "Zapatillas", CreatedBy = TenantId, CreatedByName = "Test User" });
        ctx.Colors.Add(new Color { Id = ColorId, Name = "Negro", CreatedBy = TenantId, CreatedByName = "Test User" });
        ctx.Sizes.Add(new Size { Id = SizeId, Name = "42", SortOrder = 1, CreatedBy = TenantId, CreatedByName = "Test User" });
        ctx.Providers.Add(Provider.Create("Proveedor", TenantId, UserId, "Test User"));
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static Product SeedProduct(TestAppDbContext ctx, string name, string code, bool isActive)
    {
        var product = Product.Create(name, "desc", CategoryId, BrandId, Gender.Unisex, code, TenantId, UserId, "Test User");
        product.IsActive = isActive;
        ctx.Products.Add(product);
        ctx.SaveChangesAsync().GetAwaiter().GetResult();

        var variant = ProductVariant.Create(product.Id, ColorId, SizeId, 100m, $"{code}-001", TenantId, UserId, "Test User");
        ctx.ProductVariants.Add(variant);
        ctx.SaveChangesAsync().GetAwaiter().GetResult();

        return product;
    }

    private static Product SeedProductWithStock(TestAppDbContext ctx, string name, string code, int stock)
    {
        var product = SeedProduct(ctx, name, code, isActive: true);

        var variantId = ctx.ProductVariants
            .Where(v => v.ProductId == product.Id)
            .Select(v => v.Id)
            .Single();

        ctx.BranchInventories.Add(new BranchInventory
        {
            ProductVariantId = variantId,
            BranchId = BranchId,
            Stock = stock,
            CreatedBy = UserId,
            CreatedByName = "Test User"
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();

        return product;
    }

    [Fact]
    public async Task GetProducts_ShouldReturnOnlyActive_ByDefault()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var active = SeedProduct(ctx, "Active Product", "PRD-1", isActive: true);
        SeedProduct(ctx, "Inactive Product", "PRD-2", isActive: false);

        var sut = new GetProductsUc(ctx);
        var result = await sut.Execute(CreateActorContext(), new ProductQueryDto { Page = 1, PageSize = 20 });

        Assert.True(result.IsSuccess);
        var item = result.Value.Items.Single();
        Assert.Equal(active.Id, item.Id);
        Assert.True(item.IsActive);
    }

    [Fact]
    public async Task GetProducts_ShouldReturnInactive_WhenIncludeInactive()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        SeedProduct(ctx, "Active Product", "PRD-1", isActive: true);
        SeedProduct(ctx, "Inactive Product", "PRD-2", isActive: false);

        var sut = new GetProductsUc(ctx);
        var result = await sut.Execute(CreateActorContext(), new ProductQueryDto { Page = 1, PageSize = 20, IncludeInactive = true });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, i => !i.IsActive);
    }

    [Fact]
    public async Task GetProducts_ShouldSortByStockAscending()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var low = SeedProductWithStock(ctx, "Low Stock", "PRD-1", 5);
        var high = SeedProductWithStock(ctx, "High Stock", "PRD-2", 20);

        var sut = new GetProductsUc(ctx);
        var result = await sut.Execute(CreateActorContext(), new ProductQueryDto
        {
            Page = 1,
            PageSize = 20,
            SortBy = ProductSortBy.Stock,
            SortDescending = false
        });

        Assert.True(result.IsSuccess);
        var items = result.Value.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(low.Id, items[0].Id);
        Assert.Equal(high.Id, items[1].Id);
    }

    [Fact]
    public async Task GetProducts_ShouldSortByStockDescending_ByDefault()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var low = SeedProductWithStock(ctx, "Low Stock", "PRD-1", 5);
        var high = SeedProductWithStock(ctx, "High Stock", "PRD-2", 20);

        var sut = new GetProductsUc(ctx);
        var result = await sut.Execute(CreateActorContext(), new ProductQueryDto
        {
            Page = 1,
            PageSize = 20,
            SortBy = ProductSortBy.Stock
        });

        Assert.True(result.IsSuccess);
        var items = result.Value.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(high.Id, items[0].Id);
        Assert.Equal(low.Id, items[1].Id);
    }

    [Fact]
    public async Task GetProductVariantByCode_ShouldReturnProductInactive_WhenProductIsInactive()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var product = SeedProduct(ctx, "Inactive Product", "PRD-1", isActive: false);

        var sut = new GetProductVariantByCode(ctx);
        var result = await sut.Execute(CreateActorContext(), $"{product.InternalCode}-001");

        Assert.False(result.IsSuccess);
        Assert.Equal(GetProductVariantByCodeErrors.ProductInactive, result.Error);
    }

    [Fact]
    public async Task CreateReception_ShouldReturnProductInactive_WhenItemProductIsInactive()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var product = SeedProduct(ctx, "Inactive Product", "PRD-1", isActive: false);
        var variant = await ctx.ProductVariants.FirstAsync(v => v.ProductId == product.Id);

        var sut = new CreateReceptionUc(ctx, new FakeInventoryNumberGenerator(), NullLogger<CreateReceptionUc>.Instance);
        var result = await sut.Execute(new ActorContext(TenantId, UserId, "Test User", BranchId, [BranchId]), new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items = [new CreateStockReceptionItemDto { ProductVariantId = variant.Id, QuantityReceived = 5, UnitCost = 50m }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateReceptionErrors.ProductInactive, result.Error);
    }

    [Fact]
    public async Task UpdateProductStatus_ShouldPersistNewState()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var product = SeedProduct(ctx, "Active Product", "PRD-1", isActive: true);

        var sut = new UpdateProductStatus(ctx);
        var result = await sut.Execute(CreateActorContext(), product.Id, new UpdateProductStatusDto { IsActive = false });

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        var updated = await ctx.Products.FindAsync(product.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task UpdateProductStatus_ShouldReturnNotFound_WhenProductMissing()
    {
        using var ctx = CreateDbContext();
        var sut = new UpdateProductStatus(ctx);

        var result = await sut.Execute(CreateActorContext(), Guid.NewGuid(), new UpdateProductStatusDto { IsActive = false });

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateProductStatusErrors.ProductNotFound, result.Error);
    }
}