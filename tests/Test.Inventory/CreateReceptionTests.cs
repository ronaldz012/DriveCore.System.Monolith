using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Organization;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Receptions;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class CreateReceptionTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProviderId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();

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

    private static async Task SeedCatalog(TestAppDbContext ctx, bool providerActive = true, bool productActive = true)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Name = "Nike", Prefix = "NIK", CreatedBy = TenantId, CreatedByName = "Test User" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Zapatillas", CreatedBy = TenantId, CreatedByName = "Test User" };
        var color = new Color { Id = Guid.NewGuid(), Name = "Negro", CreatedBy = TenantId, CreatedByName = "Test User" };
        var size = new Size { Id = Guid.NewGuid(), Name = "42", SortOrder = 1, CreatedBy = TenantId, CreatedByName = "Test User" };
        ctx.Brands.Add(brand);
        ctx.Categories.Add(category);
        ctx.Colors.Add(color);
        ctx.Sizes.Add(size);

        var provider = Provider.Create("Proveedor", TenantId, UserId, "Test User");
        provider.Id = ProviderId;
        provider.IsActive = providerActive;
        ctx.Providers.Add(provider);

        var product = Product.Create("Air Max", "d", category.Id, brand.Id, Gender.Unisex, "NIK-1", TenantId, UserId, "Test User");
        product.IsActive = productActive;
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var variant = ProductVariant.Create(product.Id, color.Id, size.Id, 100m, "NIK-1-001", TenantId, UserId, "Test User");
        variant.Id = VariantId;
        ctx.ProductVariants.Add(variant);
        await ctx.SaveChangesAsync();

        ctx.BranchInventories.Add(new BranchInventory
        {
            ProductVariantId = VariantId,
            BranchId = BranchId,
            Stock = 5,
            CreatedBy = UserId,
            CreatedByName = "Test User"
        });
        await ctx.SaveChangesAsync();
    }

    private static CreateStockReceptionDto CreateDto(int quantity)
    {
        return new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items = [new CreateStockReceptionItemDto { ProductVariantId = VariantId, QuantityReceived = quantity, UnitCost = 50m }]
        };
    }

    private static CreateReceptionUc CreateSut(TestAppDbContext ctx)
        => new(ctx, new FakeInventoryNumberGenerator(), NullLogger<CreateReceptionUc>.Instance);

    [Fact]
    public async Task Execute_ShouldIncreaseStock_AndCreateReceptionMovement()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(5));

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(10, inventory.Stock);

        var reception = await ctx.StockReceptions.SingleAsync();
        Assert.Equal(ReceptionStatus.Confirmed, reception.Status);
        Assert.Single(reception.Items);

        var movement = await ctx.StockMovements.SingleAsync(m => m.ReferenceId == reception.Id);
        Assert.Equal(MovementType.Reception, movement.MovementType);
        Assert.Equal(5m, movement.Quantity);
        Assert.Equal(5, movement.StockBefore);
        Assert.Equal(10, movement.StockAfter);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenVariantNotFound()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items = [new CreateStockReceptionItemDto { ProductVariantId = Guid.NewGuid(), QuantityReceived = 1, UnitCost = 50m }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateReceptionErrors.VariantsNotFound, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenProviderInactive()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx, providerActive: false);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateReceptionErrors.ProviderInactive, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenProviderNotFound()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new CreateStockReceptionDto
        {
            ProviderId = Guid.NewGuid(),
            Items = [new CreateStockReceptionItemDto { ProductVariantId = VariantId, QuantityReceived = 1, UnitCost = 50m }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateReceptionErrors.ProviderNotFound, result.Error);
    }
}