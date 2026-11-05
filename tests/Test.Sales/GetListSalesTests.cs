using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Get;
using Module.Sales.Domain;
using Moq;

namespace Test.Sales;

public class GetListSalesTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();
    private static readonly Guid SaleId = Guid.NewGuid();
    private static readonly Guid ReturnId = Guid.NewGuid();

    [Fact]
    public async Task Execute_ShouldReturnSales_WhenNoFilter()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithReturn(dbContext);

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(SaleType.Sale, result.Value.Items.First().Type);
    }

    [Fact]
    public async Task Execute_ShouldFilterSalesWithReturn_WhenHasReturnTrue()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithReturn(dbContext);

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale, HasReturn = true });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.True(result.Value.Items.First().HasReturn);
        Assert.Equal(50m, result.Value.Items.First().ReturnedAmount);
    }

    [Fact]
    public async Task Execute_ShouldFilterSalesWithoutReturn_WhenHasReturnFalse()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithoutReturn(dbContext);

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { HasReturn = false });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.False(result.Value.Items.First().HasReturn);
    }

    [Fact]
    public async Task Execute_ShouldExcludeReturns_WhenQueryingSales()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedSaleWithReturn(dbContext);

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale });

        Assert.True(result.IsSuccess);
        // Only the Sale should be listed, not the Return
        Assert.Single(result.Value.Items);
        Assert.Equal(SaleId, result.Value.Items.First().Id);
    }

    [Fact]
    public async Task Execute_ShouldMatchSaleByNumber_WhenFilterIsNumeric()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        dbContext.Sales.Add(BuildSale(number: 123));
        await dbContext.SaveChangesAsync();

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Filter = "123" });

        Assert.True(result.IsSuccess);
        Assert.Equal(123, Assert.Single(result.Value.Items).Number);
    }

    [Fact]
    public async Task Execute_ShouldMatchSaleByNumericSku_WhenFilterIsNumeric()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        var sale = BuildSale(number: 5, sku: "32193");
        dbContext.Sales.Add(sale);
        await dbContext.SaveChangesAsync();

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Filter = "32193" });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, Assert.Single(result.Value.Items).Number);
    }

    [Fact]
    public async Task Execute_ShouldReportNoneStatus_WhenSaleHasNoReturns()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        dbContext.Sales.Add(BuildSale());
        await dbContext.SaveChangesAsync();

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale });

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleReturnStatus.None, Assert.Single(result.Value.Items).ReturnStatus);
    }

    [Fact]
    public async Task Execute_ShouldReportPartialStatus_WhenSomeUnitsReturned()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        var sale = BuildSale(quantity: 2);
        dbContext.Sales.Add(sale);
        dbContext.Sales.Add(BuildReturn(sale, returnedQuantity: 1));
        await dbContext.SaveChangesAsync();

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale });

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleReturnStatus.Partial, Assert.Single(result.Value.Items).ReturnStatus);
    }

    [Fact]
    public async Task Execute_ShouldReportFullStatus_WhenAllUnitsReturned()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        var sale = BuildSale(quantity: 2);
        dbContext.Sales.Add(sale);
        dbContext.Sales.Add(BuildReturn(sale, returnedQuantity: 2));
        await dbContext.SaveChangesAsync();

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale });

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleReturnStatus.Full, Assert.Single(result.Value.Items).ReturnStatus);
    }

    [Fact]
    public async Task Execute_ShouldFilterByReturnStatus_WhenReturnStatusProvided()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        var fullyReturned = BuildSale(number: 10, quantity: 2);
        var untouched = BuildSale(number: 11);
        dbContext.Sales.Add(fullyReturned);
        dbContext.Sales.Add(untouched);
        dbContext.Sales.Add(BuildReturn(fullyReturned, returnedQuantity: 2));
        await dbContext.SaveChangesAsync();

        var sut = new GetListSales(dbContext);

        var full = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale, ReturnStatus = SaleReturnStatus.Full });
        var none = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale, ReturnStatus = SaleReturnStatus.None });
        var partial = await sut.Execute(CreateActorContext(), new SalesQueryDto { Type = SaleType.Sale, ReturnStatus = SaleReturnStatus.Partial });

        Assert.True(full.IsSuccess && none.IsSuccess && partial.IsSuccess);
        Assert.Equal(10, Assert.Single(full.Value!.Items).Number);
        Assert.Equal(11, Assert.Single(none.Value!.Items).Number);
        Assert.Empty(partial.Value!.Items);
    }

    [Fact]
    public async Task Execute_ShouldPopulateOriginalSaleNumber_OnReturnRows()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        var sale = BuildSale(number: 77, quantity: 2);
        dbContext.Sales.Add(sale);
        dbContext.Sales.Add(BuildReturn(sale, returnedQuantity: 1, number: 78));
        await dbContext.SaveChangesAsync();

        var sut = new GetListSales(dbContext);

        var result = await sut.Execute(CreateActorContext(), new SalesQueryDto());

        Assert.True(result.IsSuccess);
        var returnRow = Assert.Single(result.Value.Items, i => i.Type == SaleType.Return);
        Assert.Equal(77, returnRow.OriginalSaleNumber);
        Assert.Equal(SaleReturnStatus.None, returnRow.ReturnStatus);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static Sale BuildSale(int number = 1, int quantity = 2, string sku = "SKU-001", string displayName = "Test Product")
    {
        var saleId = Guid.NewGuid();
        var sale = new Sale
        {
            Id = saleId,
            Number = number,
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Test User",
            CreatedBy = UserId,
            CreatedByName = "Test User",
            CashRegisterClosureId = CashClosureId,
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Sale,
            TotalAmount = 50m * quantity,
            TenantId = TenantId
        };

        sale.SaleItems.Add(new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = saleId,
            ProductVariantId = Guid.NewGuid(),
            ProductSku = sku,
            ProductDisplayName = displayName,
            Quantity = quantity,
            UnitPrice = 50m,
            UnitCost = 30m,
            DiscountAmount = 0,
            FinalPrice = 50m * quantity,
            TenantId = TenantId
        });

        return sale;
    }

    private static Sale BuildReturn(Sale original, int returnedQuantity, int number = 99)
    {
        var originalItem = original.SaleItems.First();
        var returnId = Guid.NewGuid();
        var returnSale = new Sale
        {
            Id = returnId,
            Number = number,
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Test User",
            CreatedBy = UserId,
            CreatedByName = "Test User",
            CashRegisterClosureId = CashClosureId,
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Return,
            OriginalSaleId = original.Id,
            TotalAmount = -originalItem.UnitPrice * returnedQuantity,
            TenantId = TenantId
        };

        returnSale.SaleItems.Add(new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = returnId,
            OriginalSaleItemId = originalItem.Id,
            ProductVariantId = originalItem.ProductVariantId,
            ProductSku = originalItem.ProductSku,
            ProductDisplayName = originalItem.ProductDisplayName,
            Quantity = -returnedQuantity,
            UnitPrice = originalItem.UnitPrice,
            UnitCost = originalItem.UnitCost,
            DiscountAmount = 0,
            FinalPrice = -originalItem.UnitPrice * returnedQuantity,
            TenantId = TenantId
        });

        return returnSale;
    }

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
            PaymentMethod = PaymentMethod.Cash,
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
            PaymentMethod = PaymentMethod.Cash,
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
            PaymentMethod = PaymentMethod.Cash,
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
