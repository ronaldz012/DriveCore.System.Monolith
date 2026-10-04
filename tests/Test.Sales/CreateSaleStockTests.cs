using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Products;
using Module.Inventory.Infrastructure;
using Module.Sales.Application.UseCases.Sales.Create;
using Module.Sales.Domain;
using System.Infrastructure.Persistence;

namespace Test.Sales;

public class CreateSaleStockTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();

    private static TenantConnectionContext CreateTenantContext()
        => TestSalesDbContextFactory.CreateTenantContext(TenantId);

    private static AppDbContext CreateDbContext(ITenantConnectionContext tenant, string? dbName = null)
        => TestSalesDbContextFactory.Create(tenant, dbName ?? $"SalesStock_{Guid.NewGuid()}");

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static void SeedCatalog(AppDbContext ctx)
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
        ctx.SaveChangesAsync().GetAwaiter().GetResult();

        var variant = ProductVariant.Create(product.Id, color.Id, size.Id, 100m, "NIK-1-001", TenantId, UserId, "Test User");
        variant.Id = VariantId;
        ctx.ProductVariants.Add(variant);
        ctx.SaveChangesAsync().GetAwaiter().GetResult();

        ctx.BranchInventories.Add(new BranchInventory
        {
            ProductVariantId = VariantId,
            BranchId = BranchId,
            Stock = 10,
            MinStock = 0,
            CreatedBy = UserId,
            CreatedByName = "Test User"
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();

        ctx.CashRegisterClosures.Add(new CashRegisterClosure
        {
            Id = CashClosureId,
            BranchId = BranchId,
            IsOpen = true,
            OpenAt = DateTime.UtcNow,
            OpeningBalance = 500m,
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static CreateSale CreateSut(AppDbContext ctx)
    {
        var inventoryService = new InventoryIntegrationService(ctx);
        return new CreateSale(ctx, inventoryService, new FakeSalesNumberGenerator(), Mock.Of<ILogger<CreateSale>>());
    }

    private static CreateSaleDto CreateDto(int quantity)
    {
        return new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Items = [new CreateSaleItemDto { ProductVariantId = VariantId, Quantity = quantity }]
        };
    }

    [Fact]
    public async Task Execute_WithRealInventory_ShouldDeductStockAndPersistSale()
    {
        var tenant = CreateTenantContext();
        using var ctx = CreateDbContext(tenant, "sale_e2e");
        SeedCatalog(ctx);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(3));

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var sale = await ctx.Sales.FirstAsync();
        Assert.Equal(300m, sale.TotalAmount);
        Assert.Single(sale.SaleItems);

        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(7, inventory.Stock);

        var movement = await ctx.StockMovements.SingleAsync(m => m.ReferenceId == sale.Id);
        Assert.Equal(MovementType.Sale, movement.MovementType);
        Assert.Equal(-3m, movement.Quantity);
        Assert.Equal(BranchId, movement.BranchId);
        Assert.Equal(10, movement.StockBefore);
        Assert.Equal(7, movement.StockAfter);
    }

    [Fact]
    public async Task Execute_WithRealInventory_ShouldNotPersist_WhenInsufficientStock()
    {
        var tenant = CreateTenantContext();
        using var ctx = CreateDbContext(tenant, "sale_insufficient");
        SeedCatalog(ctx);
        var sut = CreateSut(ctx);

        var result = await sut.Execute(CreateActorContext(), CreateDto(50));

        Assert.False(result.IsSuccess);

        Assert.Empty(await ctx.Sales.ToListAsync());
        Assert.Empty(await ctx.StockMovements.ToListAsync());

        var inventory = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(10, inventory.Stock);
    }
}