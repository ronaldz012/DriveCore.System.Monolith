using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Application.UseCases.Receptions.Revert;
using Module.Inventory.Domain.Organization;
using Module.Inventory.Domain.Products;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class AverageCostTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProviderId = Guid.NewGuid();
    private static readonly Guid VariantA = Guid.NewGuid();
    private static readonly Guid VariantB = Guid.NewGuid();

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

    private static async Task SeedCatalog(TestAppDbContext ctx)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Name = "Nike", Prefix = "NIK", CreatedBy = TenantId, CreatedByName = "Test User" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Zapatillas", CreatedBy = TenantId, CreatedByName = "Test User" };
        var color = new Color { Id = Guid.NewGuid(), Name = "Negro", CreatedBy = TenantId, CreatedByName = "Test User" };
        var sizeA = new Size { Id = Guid.NewGuid(), Name = "42", SortOrder = 1, CreatedBy = TenantId, CreatedByName = "Test User" };
        var sizeB = new Size { Id = Guid.NewGuid(), Name = "43", SortOrder = 2, CreatedBy = TenantId, CreatedByName = "Test User" };
        ctx.Brands.Add(brand);
        ctx.Categories.Add(category);
        ctx.Colors.Add(color);
        ctx.Sizes.Add(sizeA);
        ctx.Sizes.Add(sizeB);

        var provider = Provider.Create("Proveedor", TenantId, UserId, "Test User");
        provider.Id = ProviderId;
        ctx.Providers.Add(provider);

        var product = Product.Create("Air Max", "d", category.Id, brand.Id, Gender.Unisex, "NIK-1", TenantId, UserId, "Test User");
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var variantA = ProductVariant.Create(product.Id, color.Id, sizeA.Id, 100m, "NIK-1-001", TenantId, UserId, "Test User");
        variantA.Id = VariantA;
        ctx.ProductVariants.Add(variantA);

        var variantB = ProductVariant.Create(product.Id, color.Id, sizeB.Id, 100m, "NIK-1-002", TenantId, UserId, "Test User");
        variantB.Id = VariantB;
        ctx.ProductVariants.Add(variantB);

        await ctx.SaveChangesAsync();
    }

    private static CreateReceptionUc CreateSut(TestAppDbContext ctx)
        => new(ctx, new FakeInventoryNumberGenerator(), NullLogger<CreateReceptionUc>.Instance);

    private static async Task<decimal> GetAverageCost(TestAppDbContext ctx, Guid variantId)
        => (await ctx.ProductVariants.SingleAsync(pv => pv.Id == variantId)).AverageCost;

    [Fact]
    public async Task TwoReceptions_ShouldEvolveAverageCostPerVariant()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var sut = CreateSut(ctx);

        // 1ª recepción: A 10u @10, B 5u @20 (stock previo = 0, avg previo = 0)
        var first = await sut.Execute(CreateActorContext(), new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items =
            [
                new CreateStockReceptionItemDto { ProductVariantId = VariantA, QuantityReceived = 10, UnitCost = 10m },
                new CreateStockReceptionItemDto { ProductVariantId = VariantB, QuantityReceived = 5, UnitCost = 20m }
            ]
        });
        Assert.True(first.IsSuccess, $"Expected success but got: {first.Error?.Code} - {first.Error?.Message}");

        // A: (0*0 + 10*10)/10 = 10
        Assert.Equal(10m, await GetAverageCost(ctx, VariantA));
        // B: (0*0 + 5*20)/5 = 20
        Assert.Equal(20m, await GetAverageCost(ctx, VariantB));

        // 2ª recepción: A 10u @30, B 5u @10
        var second = await sut.Execute(CreateActorContext(), new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items =
            [
                new CreateStockReceptionItemDto { ProductVariantId = VariantA, QuantityReceived = 10, UnitCost = 30m },
                new CreateStockReceptionItemDto { ProductVariantId = VariantB, QuantityReceived = 5, UnitCost = 10m }
            ]
        });
        Assert.True(second.IsSuccess, $"Expected success but got: {second.Error?.Code} - {second.Error?.Message}");

        // A: (10*10 + 30*10)/20 = 20
        Assert.Equal(20m, await GetAverageCost(ctx, VariantA));
        // B: (20*5 + 10*5)/10 = 15
        Assert.Equal(15m, await GetAverageCost(ctx, VariantB));

        // El stock final también debe ser correcto
        Assert.Equal(20, (await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantA)).Stock);
        Assert.Equal(10, (await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantB)).Stock);

        // 4 movimientos de recepción con su UnitCost snapshot correcto
        Assert.Equal(4, await ctx.StockMovements.CountAsync());
        Assert.NotNull(await ctx.StockMovements.SingleOrDefaultAsync(m => m.ProductVariantId == VariantA && m.UnitCost == 30m));
        Assert.NotNull(await ctx.StockMovements.SingleOrDefaultAsync(m => m.ProductVariantId == VariantB && m.UnitCost == 10m));
    }

    [Fact]
    public async Task ThreeReceptions_ThenRevertMiddle_ShouldLeaveAverageAsFirstPlusLast()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var createSut = CreateSut(ctx);
        var actor = CreateActorContext();

        // A: 10u @10
        var a = await createSut.Execute(actor, new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items = [new CreateStockReceptionItemDto { ProductVariantId = VariantA, QuantityReceived = 10, UnitCost = 10m }]
        });
        Assert.True(a.IsSuccess);

        // B: 10u @50
        var b = await createSut.Execute(actor, new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items = [new CreateStockReceptionItemDto { ProductVariantId = VariantA, QuantityReceived = 10, UnitCost = 50m }]
        });
        Assert.True(b.IsSuccess);

        // C: 10u @10
        var c = await createSut.Execute(actor, new CreateStockReceptionDto
        {
            ProviderId = ProviderId,
            Items = [new CreateStockReceptionItemDto { ProductVariantId = VariantA, QuantityReceived = 10, UnitCost = 10m }]
        });
        Assert.True(c.IsSuccess);

        // Promedio con A+B+C = (10*10 + 10*50 + 10*10)/30 = 23.33
        Assert.Equal(700m / 30m, await GetAverageCost(ctx, VariantA));

        var revertSut = new RevertStockReception(ctx, NullLogger<RevertStockReception>.Instance);
        var revert = await revertSut.Execute(actor, b.Value!.Id);
        Assert.True(revert.IsSuccess, $"Expected revert success but got: {revert.Error?.Code} - {revert.Error?.Message}");

        // Al revertir B, el promedio debe quedar como A+C: (10*10 + 10*10)/20 = 10
        Assert.Equal(10m, await GetAverageCost(ctx, VariantA));

        // Stock final = 30 - 10 = 20
        Assert.Equal(20, (await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantA)).Stock);
    }
}