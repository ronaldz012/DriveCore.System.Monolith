using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Return.List;
using Module.Sales.Domain;
using Moq;

namespace Test.Sales;

public class ListReturnsTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();
    private static readonly Guid SaleId = Guid.NewGuid();
    private static readonly Guid ReturnId = Guid.NewGuid();

    [Fact]
    public async Task Execute_ShouldReturnOnlyReturns_WhenQuerying()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithReturn(dbContext);

        var sut = new ListReturns(dbContext);

        var result = await sut.Execute(CreateActorContext(), new ReturnsQueryDto());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(ReturnId, result.Value.Items.First().Id);
    }

    [Fact]
    public async Task Execute_ShouldReturnEmpty_WhenNoReturnsExist()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithoutReturn(dbContext);

        var sut = new ListReturns(dbContext);

        var result = await sut.Execute(CreateActorContext(), new ReturnsQueryDto());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task Execute_ShouldReturnOriginalSaleId_WhenReturnExists()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithReturn(dbContext);

        var sut = new ListReturns(dbContext);

        var result = await sut.Execute(CreateActorContext(), new ReturnsQueryDto());

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleId, result.Value.Items.First().OriginalSaleId);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static void SeedSaleWithReturn(ISalesDbContext ctx)
    {
        ctx.Sales.Add(new Sale
        {
            Id = SaleId,
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
                    ProductSku = "SKU-001",
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
            Id = ReturnId,
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Test User",
            CreatedBy = UserId,
            CreatedByName = "Test User",
            CashRegisterClosureId = CashClosureId,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Return,
            OriginalSaleId = SaleId,
            TotalAmount = -50m,
            TenantId = TenantId,
            SaleItems =
            [
                new SaleItem
                {
                    ProductVariantId = Guid.NewGuid(),
                    ProductSku = "SKU-001",
                    ProductDisplayName = "Test Product",
                    Quantity = -1,
                    UnitPrice = 50m,
                    UnitCost = 30m,
                    DiscountAmount = 0,
                    FinalPrice = -50m,
                    OriginalSaleItemId = null,
                    TenantId = TenantId
                }
            ]
        });

        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static void SeedSaleWithoutReturn(ISalesDbContext ctx)
    {
        ctx.Sales.Add(new Sale
        {
            Id = SaleId,
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
                    ProductSku = "SKU-001",
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
}
