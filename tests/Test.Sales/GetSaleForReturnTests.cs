using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Return;
using Module.Sales.Application.UseCases.Sales.Return.GetSaleForReturn;
using Module.Sales.Domain;
using Moq;

namespace Test.Sales;

public class GetSaleForReturnTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();
    private static readonly Guid SaleId = Guid.NewGuid();

    [Fact]
    public async Task Execute_ShouldReturnSaleWithAllItems_WhenValidRequest()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSale(dbContext);

        var sut = new GetSaleForReturn(dbContext);

        var result = await sut.Execute(CreateActorContext(), SaleId);

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleId, result.Value.Id);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.All(result.Value.Items, i => Assert.Equal(2, i.ReturnableQuantity));
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenSaleNotFound()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var sut = new GetSaleForReturn(dbContext);

        var result = await sut.Execute(CreateActorContext(), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.OriginalSaleNotFound, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenSaleIsNotSaleType()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSale(dbContext, SaleType.Return);

        var sut = new GetSaleForReturn(dbContext);

        var result = await sut.Execute(CreateActorContext(), SaleId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.OriginalSaleNotEligible, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenAlreadyReturned()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSale(dbContext);
        SeedReturn(dbContext);

        var sut = new GetSaleForReturn(dbContext);

        var result = await sut.Execute(CreateActorContext(), SaleId);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.AlreadyReturned, result.Error);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static void SeedSale(ISalesDbContext ctx, SaleType type = SaleType.Sale)
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
            Type = type,
            TotalAmount = 100m,
            TenantId = TenantId,
            SaleItems =
            [
                new SaleItem
                {
                    ProductVariantId = Guid.NewGuid(),
                    ProductSku = "SKU-001",
                    ProductDisplayName = "Test Product A",
                    Quantity = 2,
                    UnitPrice = 25m,
                    UnitCost = 15m,
                    DiscountAmount = 0,
                    FinalPrice = 50m,
                    TenantId = TenantId
                },
                new SaleItem
                {
                    ProductVariantId = Guid.NewGuid(),
                    ProductSku = "SKU-002",
                    ProductDisplayName = "Test Product B",
                    Quantity = 2,
                    UnitPrice = 25m,
                    UnitCost = 15m,
                    DiscountAmount = 0,
                    FinalPrice = 50m,
                    TenantId = TenantId
                }
            ]
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static void SeedReturn(ISalesDbContext ctx)
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
            Type = SaleType.Return,
            OriginalSaleId = SaleId,
            TotalAmount = -50m,
            TenantId = TenantId
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
