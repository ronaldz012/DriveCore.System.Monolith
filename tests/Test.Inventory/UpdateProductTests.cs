using System.ComponentModel.DataAnnotations;
using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Module.Inventory.Application.UseCases.Products.Update;
using Module.Inventory.Domain.Products;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class UpdateProductTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid BrandId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid Variant1Id = Guid.NewGuid();
    private static readonly Guid Variant2Id = Guid.NewGuid();

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", Guid.Empty, []);

    private static TestAppDbContext CreateDbContext()
    {
        var tenantCtx = new TestTenantConnectionContext
        {
            TenantId = TenantId,
            Schema = "test_schema",
            DatabaseName = "test_db"
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UpdProdTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestAppDbContext(options, tenantCtx);
    }

    private static void SeedCatalog(TestAppDbContext ctx)
    {
        ctx.Brands.Add(new Brand { Id = BrandId, Name = "Test Brand", Prefix = "BRD", ProductCounter = 0, CreatedBy = TenantId, CreatedByName = "Test User" });
        ctx.Categories.Add(new Category { Id = CategoryId, Name = "Test Category", CreatedBy = TenantId, CreatedByName = "Test User" });
        var product = Product.Create("Test Product", "d", CategoryId, BrandId, Gender.Unisex, "BRD-1", TenantId, UserId, "Test User");
        product.Id = ProductId;
        ctx.Products.Add(product);

        var v1 = ProductVariant.Create(ProductId, Guid.NewGuid(), Guid.NewGuid(), 100m, "BRD-1-001", TenantId, UserId, "Test User");
        v1.Id = Variant1Id;
        var v2 = ProductVariant.Create(ProductId, Guid.NewGuid(), Guid.NewGuid(), 200m, "BRD-1-002", TenantId, UserId, "Test User");
        v2.Id = Variant2Id;
        ctx.ProductVariants.AddRange(v1, v2);
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
        ctx.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Execute_ShouldUpdateVariantPrices_WhenBulkIsValid()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var sut = new UpdateProduct(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductDto
        {
            VariantPrices =
            [
                new VariantPriceItem { VariantId = Variant1Id, Price = 150m },
                new VariantPriceItem { VariantId = Variant2Id, Price = 250m }
            ]
        }, ProductId);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");
        Assert.Equal(150m, (await ctx.ProductVariants.FindAsync(Variant1Id))!.Price);
        Assert.Equal(250m, (await ctx.ProductVariants.FindAsync(Variant2Id))!.Price);
    }

    [Fact]
    public async Task Execute_ShouldIgnorePrices_WhenVariantPricesIsEmpty()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var sut = new UpdateProduct(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductDto { Name = "Renamed" }, ProductId);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, (await ctx.ProductVariants.FindAsync(Variant1Id))!.Price);
        Assert.Equal("Renamed", (await ctx.Products.FindAsync(ProductId))!.Name);
    }

    [Fact]
    public async Task Execute_ShouldFailAll_WhenVariantBelongsToAnotherProduct()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);

        var other = ProductVariant.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 999m, "XXX-1", TenantId, UserId, "Test User");
        var foreignId = other.Id;
        ctx.ProductVariants.Add(other);
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var sut = new UpdateProduct(ctx);
        var result = await sut.Execute(CreateActorContext(), new UpdateProductDto
        {
            Name = "Renamed",
            VariantPrices =
            [
                new VariantPriceItem { VariantId = Variant1Id, Price = 150m },
                new VariantPriceItem { VariantId = foreignId, Price = 50m }
            ]
        }, ProductId);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateProductErrors.VariantNotFound, result.Error);
        var reloadedVariant = (await ctx.ProductVariants.FindAsync(Variant1Id))!;
        var reloadedProduct = (await ctx.Products.FindAsync(ProductId))!;
        await ctx.Entry(reloadedVariant).ReloadAsync();
        await ctx.Entry(reloadedProduct).ReloadAsync();
        Assert.Equal(100m, reloadedVariant.Price);
        Assert.Equal("Test Product", reloadedProduct.Name);
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenVariantDoesNotExist()
    {
        using var ctx = CreateDbContext();
        SeedCatalog(ctx);
        var sut = new UpdateProduct(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductDto
        {
            VariantPrices = [new VariantPriceItem { VariantId = Guid.NewGuid(), Price = 150m }]
        }, ProductId);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateProductErrors.VariantNotFound, result.Error);
    }

    [Fact]
    public void Validate_ShouldFail_WhenDuplicateVariantIds()
    {
        var dto = new UpdateProductDto
        {
            VariantPrices =
            [
                new VariantPriceItem { VariantId = Variant1Id, Price = 150m },
                new VariantPriceItem { VariantId = Variant1Id, Price = 160m }
            ]
        };

        var results = dto.Validate(new ValidationContext(dto)).ToList();

        Assert.Single(results);
    }
}
