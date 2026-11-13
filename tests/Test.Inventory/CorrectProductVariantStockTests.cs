using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Module.Inventory.Application.UseCases.ProductVariants.PatchStock;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Products;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class CorrectProductVariantStockTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
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

    private static async Task SeedVariant(TestAppDbContext ctx, int stock)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Name = "Nike", Prefix = "NIK", CreatedBy = TenantId, CreatedByName = "Test User" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Zapatillas", CreatedBy = TenantId, CreatedByName = "Test User" };
        var color = new Color { Id = Guid.NewGuid(), Name = "Negro", CreatedBy = TenantId, CreatedByName = "Test User" };
        var size = new Size { Id = Guid.NewGuid(), Name = "42", SortOrder = 1, CreatedBy = TenantId, CreatedByName = "Test User" };
        ctx.Brands.Add(brand);
        ctx.Categories.Add(category);
        ctx.Colors.Add(color);
        ctx.Sizes.Add(size);

        var product = Product.Create("Air Max", "d", category.Id, brand.Id, Gender.Unisex, "NIK-1", TenantId, UserId, "Test User");
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
            Stock = stock,
            CreatedBy = UserId,
            CreatedByName = "Test User"
        });
        await ctx.SaveChangesAsync();
    }

    private static CorrectProductVariantStock CreateSut(TestAppDbContext ctx)
        => new(ctx);

    private static async Task<decimal> MovementSum(TestAppDbContext ctx)
        => await ctx.StockMovements
            .Where(m => m.ProductVariantId == VariantId && m.BranchId == BranchId)
            .SumAsync(m => m.Quantity);

    [Fact]
    public async Task Execute_ShouldSetStock_AndCreateAdjustmentWithDelta()
    {
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductVariantStockDto { Stock = 7, Notes = "Ajuste" }, VariantId);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(7, inventory.Stock);

        var movement = await ctx.StockMovements.SingleAsync();
        Assert.Equal(MovementType.Adjustment, movement.MovementType);
        Assert.Equal(-3m, movement.Quantity);
        Assert.Equal(10, movement.StockBefore);
        Assert.Equal(7, movement.StockAfter);
        Assert.Equal(-3m, await MovementSum(ctx));
        Assert.Equal(7m, 10m + await MovementSum(ctx));
    }

    [Fact]
    public async Task Execute_ShouldCreateNegativeAdjustment_WhenDecreasing()
    {
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductVariantStockDto { Stock = 3, Notes = "Baja" }, VariantId);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, (await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId)).Stock);
        Assert.Equal(-7m, await MovementSum(ctx));
        var movement = await ctx.StockMovements.SingleAsync();
        Assert.Equal(10, movement.StockBefore);
        Assert.Equal(3, movement.StockAfter);
    }

    [Fact]
    public async Task Execute_ShouldNotCreateMovement_WhenStockUnchanged()
    {
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductVariantStockDto { Stock = 10, Notes = "Sin cambio" }, VariantId);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, (await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId)).Stock);
        Assert.Empty(await ctx.StockMovements.ToListAsync());
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenVariantNotFound()
    {
        using var ctx = CreateDbContext();
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductVariantStockDto { Stock = 5, Notes = "Ajuste" }, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(CorrectProductVariantStockErrors.VariantNotFound, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenNotesMissing()
    {
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductVariantStockDto { Stock = 7, Notes = "" }, VariantId);

        Assert.False(result.IsSuccess);
        Assert.Equal(CorrectProductVariantStockErrors.StockCorrectionFailed, result.Error);
        Assert.Equal(10, (await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId)).Stock);
        Assert.Empty(await ctx.StockMovements.ToListAsync());
    }

    [Fact]
    public async Task Execute_ShouldCreatePositiveAdjustment_WhenIncreasing()
    {
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new UpdateProductVariantStockDto { Stock = 15, Notes = "Excedente" }, VariantId);

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(15, inventory.Stock);

        var movement = await ctx.StockMovements.SingleAsync();
        Assert.Equal(MovementType.Adjustment, movement.MovementType);
        Assert.Equal(5m, movement.Quantity);
        Assert.Equal(10, movement.StockBefore);
        Assert.Equal(15, movement.StockAfter);
        Assert.Equal(5m, await MovementSum(ctx));
    }
}