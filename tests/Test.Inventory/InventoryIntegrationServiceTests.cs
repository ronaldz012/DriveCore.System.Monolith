using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Products;
using Module.Inventory.Infrastructure;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class InventoryIntegrationServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid ReferenceId = Guid.NewGuid();

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

    private static InventoryIntegrationService CreateSut(TestAppDbContext ctx) => new(ctx);

    [Fact]
    public async Task DeductStock_ShouldDeductAndCreateSaleMovements()
    {
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.DeductStock(
            [new StockDeductionDto(VariantId, 3)],
            BranchId,
            UserId,
            "Test User",
            ReferenceId);

        Assert.True(result.IsSuccess);

        await ctx.SaveChangesAsync();

        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(7, inventory.Stock);

        var movement = await ctx.StockMovements.SingleAsync(m => m.ReferenceId == ReferenceId);
        Assert.Equal(MovementType.Sale, movement.MovementType);
        Assert.Equal(-3m, movement.Quantity);
        Assert.Equal(BranchId, movement.BranchId);
        Assert.Equal(10, movement.StockBefore);
        Assert.Equal(7, movement.StockAfter);
    }

    [Fact]
    public async Task DeductStock_ShouldReturnError_WhenInsufficientStock()
    {
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 2);
        var sut = CreateSut(ctx);

        var result = await sut.DeductStock(
            [new StockDeductionDto(VariantId, 5)],
            BranchId,
            UserId,
            "Test User",
            ReferenceId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCode.InvalidState, result.Error.Code);

        await ctx.SaveChangesAsync();
        Assert.Empty(await ctx.StockMovements.ToListAsync());
        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(2, inventory.Stock);
    }

    [Fact]
    public async Task DeductStock_ShouldReturnError_WhenVariantNotFound()
    {
        using var ctx = CreateDbContext();
        var sut = CreateSut(ctx);

        var result = await sut.DeductStock(
            [new StockDeductionDto(Guid.NewGuid(), 1)],
            BranchId,
            UserId,
            "Test User",
            ReferenceId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task ReturnStock_ShouldRecalculateAverageCost_AtHistoricCost()
    {
        // Caso del enunciado: stock 5 @10 -> venta 1 -> compra 3 @15 -> devolucion 1 @10
        // Control sin venta/devolucion: 5@10 + 3@15 = stock 8, avg 11.875
        // Con fix, devolucion debe dar avg 11.875; sin fix da 12.142857
        using var ctx = CreateDbContext();
        await SeedVariant(ctx, 5);
        var variant = await ctx.ProductVariants.Include(pv => pv.BranchInventories).SingleAsync(pv => pv.Id == VariantId);
        variant.AverageCost = 10m;
        await ctx.SaveChangesAsync();

        var sut = CreateSut(ctx);

        // venta 1 unidad (no toca AverageCost)
        var sell = await sut.DeductStock([new StockDeductionDto(VariantId, 1)], BranchId, UserId, "Test User", Guid.NewGuid());
        Assert.True(sell.IsSuccess);
        await ctx.SaveChangesAsync();
        Assert.Equal(10m, (await ctx.ProductVariants.SingleAsync(pv => pv.Id == VariantId)).AverageCost);

        // recepcion 3 @15 (simulada via RegisterPurchase + AddQuantity)
        variant = await ctx.ProductVariants.Include(pv => pv.BranchInventories).SingleAsync(pv => pv.Id == VariantId);
        variant.RegisterPurchase(3, 15m);
        variant.AddQuantity(3, BranchId, UserId, "Test User");
        await ctx.SaveChangesAsync();
        var afterPurchase = await ctx.ProductVariants.SingleAsync(pv => pv.Id == VariantId);
        Assert.Equal(12.1429m, afterPurchase.AverageCost); // 85/7 rounded to 4 decimals
        Assert.Equal(7, afterPurchase.BranchInventories.Sum(bi => bi.Stock));

        // devolucion 1 @ costo historico 10 (de la venta original)
        var ret = await sut.ReturnStock([new StockReturnDto(VariantId, 1, 10m)], BranchId, UserId, "Test User", Guid.NewGuid());
        Assert.True(ret.IsSuccess);
        await ctx.SaveChangesAsync();

        var afterReturn = await ctx.ProductVariants.Include(pv => pv.BranchInventories).SingleAsync(pv => pv.Id == VariantId);
        var stockAfterReturn = afterReturn.BranchInventories.Sum(bi => bi.Stock);
        Assert.Equal(8, stockAfterReturn);
        // El comportamiento correcto (b) coincide con control: 11.875
        Assert.Equal(95m / 8m, afterReturn.AverageCost, precision: 5);

        var retMovement = await ctx.StockMovements.SingleAsync(m => m.MovementType == MovementType.Return);
        Assert.Equal(1m, retMovement.Quantity);
        Assert.Equal(7, retMovement.StockBefore);
        Assert.Equal(8, retMovement.StockAfter);
    }
}