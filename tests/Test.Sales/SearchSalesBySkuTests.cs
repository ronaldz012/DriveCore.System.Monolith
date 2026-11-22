using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Search;
using Module.Sales.Domain;
using Moq;

namespace Test.Sales;

public class SearchSalesBySkuTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();
    private static readonly string Sku = "NIK-1-001";

    [Fact]
    public async Task Execute_ShouldReturnSale_WhenSkuMatches()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithSku(dbContext);

        var sut = new SearchSalesBySku(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SkuSearchQueryDto { Sku = Sku, Days = 7 });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Sales.Items);
        Assert.Equal(Sku, result.Value.SearchedSku);
        Assert.Equal("Test Product", result.Value.SearchedDisplayName);
        Assert.NotEqual(Guid.Empty, result.Value.Sales.Items.First().MatchedItem.SaleItemId);
    }

    [Fact]
    public async Task Execute_ShouldReturnEmpty_WhenSkuNotFound()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithSku(dbContext);

        var sut = new SearchSalesBySku(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SkuSearchQueryDto { Sku = "NON-EXISTENT", Days = 7 });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sales.Items);
    }

    [Fact]
    public async Task Execute_ShouldExcludeReturnedSales()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithReturn(dbContext);

        var sut = new SearchSalesBySku(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SkuSearchQueryDto { Sku = Sku, Days = 7 });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sales.Items);
    }

    [Fact]
    public async Task Execute_ShouldReturnMatchedItemDetails()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithSku(dbContext);

        var sut = new SearchSalesBySku(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SkuSearchQueryDto { Sku = Sku, Days = 7 });

        Assert.True(result.IsSuccess);
        var matchedItem = result.Value.Sales.Items.First().MatchedItem;
        Assert.NotEqual(Guid.Empty, matchedItem.SaleItemId);
        Assert.Equal(2, matchedItem.Quantity);
        Assert.Equal(50m, matchedItem.UnitPrice);
    }

    [Fact]
    public async Task Execute_ShouldExcludeSale_WhenOutsideDateRange()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithSku(dbContext, createdAt: DateTime.UtcNow.AddDays(-10));

        var sut = new SearchSalesBySku(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SkuSearchQueryDto { Sku = Sku, Days = 7 });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Sales.Items);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static void SeedSaleWithSku(ISalesDbContext ctx, DateTime? createdAt = null)
    {
        ctx.Sales.Add(new Sale
        {
            Id = Guid.NewGuid(),
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Test User",
            CreatedBy = UserId,
            CreatedByName = "Test User",
            CashRegisterClosureId = CashClosureId,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Sale,
            TotalAmount = 100m,
            TenantId = TenantId,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            SaleItems =
            [
                new SaleItem
                {
                    ProductVariantId = Guid.NewGuid(),
                    ProductSku = Sku,
                    ProductDisplayName = "Test Product",
                    Quantity = 2,
                    UnitPrice = 50m,
                    UnitCost = 30m,
                    DiscountAmount = 0,
                    FinalPrice = 100m,
                    TenantId = TenantId
                }
            ]
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static void SeedSaleWithReturn(ISalesDbContext ctx)
    {
        var saleId = Guid.NewGuid();
        ctx.Sales.Add(new Sale
        {
            Id = saleId,
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Test User",
            CreatedBy = UserId,
            CreatedByName = "Test User",
            CashRegisterClosureId = CashClosureId,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Sale,
            TotalAmount = 100m,
            TenantId = TenantId,
            SaleItems =
            [
                new SaleItem
                {
                    ProductVariantId = Guid.NewGuid(),
                    ProductSku = Sku,
                    ProductDisplayName = "Test Product",
                    Quantity = 2,
                    UnitPrice = 50m,
                    UnitCost = 30m,
                    DiscountAmount = 0,
                    FinalPrice = 100m,
                    TenantId = TenantId
                }
            ]
        });

        ctx.Sales.Add(new Sale
        {
            Id = Guid.NewGuid(),
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Test User",
            CreatedBy = UserId,
            CreatedByName = "Test User",
            CashRegisterClosureId = CashClosureId,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Return,
            OriginalSaleId = saleId,
            TotalAmount = -50m,
            TenantId = TenantId
        });

        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
