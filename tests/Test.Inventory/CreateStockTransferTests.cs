using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Module.Inventory.Application.UseCases.Transfers.Create;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Transfers;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class CreateStockTransferTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid FromBranchId = Guid.NewGuid();
    private static readonly Guid ToBranchId = Guid.NewGuid();
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
        => new(TenantId, UserId, "Test User", FromBranchId, [FromBranchId]);

    private static async Task SeedVariantWithStock(TestAppDbContext ctx, int stock, bool productActive = true)
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
            BranchId = FromBranchId,
            Stock = stock,
            CreatedBy = UserId,
            CreatedByName = "Test User"
        });
        await ctx.SaveChangesAsync();
    }

    private static CreateStockTransferDto CreateDto(int quantity, Guid toBranch)
    {
        return new CreateStockTransferDto
        {
            ToBranchId = toBranch,
            Items = [new StockTransferItemDto { ProductVariantId = VariantId, QuantityRequested = quantity }]
        };
    }

    private static CreateStockTransfer CreateSut(TestAppDbContext ctx)
        => new(ctx, new FakeInventoryNumberGenerator());

    [Fact]
    public async Task Execute_ShouldCreatePendingTransfer_WithoutMovingStock()
    {
        using var ctx = CreateDbContext();
        await SeedVariantWithStock(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(3, ToBranchId));

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var transfer = await ctx.StockTransfers.SingleAsync();
        Assert.Equal(TransferStatus.Pending, transfer.Status);
        Assert.Equal(FromBranchId, transfer.FromBranchId);
        Assert.Equal(ToBranchId, transfer.ToBranchId);
        Assert.Single(transfer.Items);

        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(10, inventory.Stock);
        Assert.Empty(await ctx.StockMovements.ToListAsync());
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenSameBranch()
    {
        using var ctx = CreateDbContext();
        await SeedVariantWithStock(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(3, FromBranchId));

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateStockTransferErrors.SameBranchTransfer, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenVariantNotInBranch()
    {
        using var ctx = CreateDbContext();
        await SeedVariantWithStock(ctx, 10);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), new CreateStockTransferDto
        {
            ToBranchId = ToBranchId,
            Items = [new StockTransferItemDto { ProductVariantId = Guid.NewGuid(), QuantityRequested = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateStockTransferErrors.VariantsNotFoundInBranch, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenInsufficientStock()
    {
        using var ctx = CreateDbContext();
        await SeedVariantWithStock(ctx, 2);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(5, ToBranchId));

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateStockTransferErrors.InsufficientStock, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenProductInactive()
    {
        using var ctx = CreateDbContext();
        await SeedVariantWithStock(ctx, 10, productActive: false);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(1, ToBranchId));

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateStockTransferErrors.ProductInactive, result.Error);
    }
}