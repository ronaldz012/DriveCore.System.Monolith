using Common.Contracts.authentication;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Stats;
using Module.Sales.Domain;

namespace Test.Sales;

public class GetSalesStatsTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();
    private static readonly DateTime Day = new(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Execute_ShouldComputeTotals_WithReturnsAndFreeItems()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(10), total: 100m,
            Registered(price: 50m, qty: 2, cost: 20m)));
        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(11), total: 30m,
            Free(price: 30m, qty: 1, name: "Pantalon diesel")));
        dbContext.Sales.Add(BuildSale(SaleType.Return, UserId, Day.AddHours(12), total: -50m,
            Registered(price: 50m, qty: -1, cost: 20m)));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var result = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: false, includeSellerBreakdown: false);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var totals = result.Value!.Totals;

        Assert.Equal(2, totals.SalesCount);
        Assert.Equal(1, totals.ReturnsCount);
        Assert.Equal(130m, totals.GrossSalesAmount);
        Assert.Equal(50m, totals.ReturnsAmount);
        Assert.Equal(80m, totals.NetSalesAmount);
        Assert.Equal(3, totals.UnitsSold);
        Assert.Equal(1, totals.UnitsReturned);
        Assert.Equal(40m, totals.AvgTicket);
        Assert.Equal(30m, totals.FreeSalesAmount);
        Assert.Equal(1, totals.FreeSalesCount);
    }

    [Fact]
    public async Task Execute_ShouldHideMargin_WhenMarginPermissionMissing()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(10), total: 100m,
            Registered(price: 50m, qty: 2, cost: 20m)));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var withMargin = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: true, includeSellerBreakdown: false);
        var withoutMargin = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: false, includeSellerBreakdown: false);

        Assert.True(withMargin.IsSuccess && withoutMargin.IsSuccess);
        Assert.Equal(60m, withMargin.Value!.Totals.GrossMargin); // (50 - 20) * 2
        Assert.Equal(100m, withMargin.Value.Totals.MarginCoveragePct);
        Assert.Null(withoutMargin.Value!.Totals.GrossMargin);
        Assert.Null(withoutMargin.Value.Totals.MarginPct);
        Assert.NotNull(withMargin.Value.TopProducts[0].GrossMargin);
        Assert.Null(withoutMargin.Value.TopProducts[0].GrossMargin);
    }

    [Fact]
    public async Task Execute_ShouldFilterBySeller_WhenSoldByIdProvided()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var otherSellerId = Guid.NewGuid();
        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(10), total: 100m, Registered(50m, 2, 20m)));
        dbContext.Sales.Add(BuildSale(SaleType.Sale, otherSellerId, Day.AddHours(11), total: 200m, Registered(100m, 2, 40m)));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var query = Range(Day);
        query.SoldById = otherSellerId;

        var result = await sut.Execute(CreateActorContext(), query, includeMargin: false, includeSellerBreakdown: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Totals.SalesCount);
        Assert.Equal(200m, result.Value.Totals.GrossSalesAmount);
    }

    [Fact]
    public async Task Execute_ShouldReturnSellerBreakdown_OnlyWhenRequested()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var otherSellerId = Guid.NewGuid();
        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(10), 100m, Registered(50m, 2, 20m)));
        dbContext.Sales.Add(BuildSale(SaleType.Sale, otherSellerId, Day.AddHours(11), 300m,
            Registered(100m, 3, 40m), soldByName: "Other Seller"));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var withBreakdown = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: false, includeSellerBreakdown: true);
        var withoutBreakdown = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: false, includeSellerBreakdown: false);

        Assert.True(withBreakdown.IsSuccess && withoutBreakdown.IsSuccess);
        Assert.Equal(2, withBreakdown.Value!.BySeller.Count);
        Assert.Equal(300m, withBreakdown.Value.BySeller[0].NetSalesAmount); // ordered desc
        Assert.Empty(withoutBreakdown.Value!.BySeller);
    }

    [Fact]
    public async Task Execute_ShouldIncludeWholeDay_WhenDateToIsDateOnly()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        // Sale late in the day: must be included when DateTo is just a date.
        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(23).AddMinutes(30), total: 100m, Registered(50m, 2, 20m)));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var result = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: false, includeSellerBreakdown: false);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Totals.SalesCount);
    }

    [Fact]
    public async Task Execute_ShouldRankTopProducts_ExcludingFreeItems()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(10), total: 300m,
            Registered(price: 100m, qty: 3, cost: 40m, name: "Air Max 90")));
        // Free items have no reliable identity to group by: they never enter the ranking.
        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, Day.AddHours(11), total: 500m,
            Free(price: 500m, qty: 1, name: "Pantalon diesel")));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var result = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: true, includeSellerBreakdown: false);

        Assert.True(result.IsSuccess);
        var top = Assert.Single(result.Value!.TopProducts);
        Assert.Equal("Air Max 90", top.ProductName);
        Assert.Equal("SKU-001", top.ProductSku);
        Assert.Equal(3, top.Units);
        Assert.Equal(300m, top.NetSalesAmount);
        Assert.Equal(180m, top.GrossMargin); // (100 - 40) * 3
        // Free sales remain visible in the totals split.
        Assert.Equal(500m, result.Value.Totals.FreeSalesAmount);
        Assert.Equal(1, result.Value.Totals.FreeSalesCount);
    }

    [Fact]
    public async Task Execute_ShouldAttributeReturnsToOriginalSaleSeller()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var sellerB = Guid.NewGuid();
        var saleA = BuildSale(SaleType.Sale, UserId, Day.AddHours(10), 100m, Registered(50m, 2, 20m));
        dbContext.Sales.Add(saleA);
        // B processes the return of A's sale (tonga/exchange case).
        dbContext.Sales.Add(BuildSale(SaleType.Return, sellerB, Day.AddHours(11), -50m,
            Registered(50m, -1, 20m), soldByName: "Seller B", originalSaleId: saleA.Id));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var queryA = Range(Day);
        queryA.SoldById = UserId;
        var resultA = await sut.Execute(CreateActorContext(), queryA, includeMargin: false, includeSellerBreakdown: false);

        var queryB = Range(Day);
        queryB.SoldById = sellerB;
        var resultB = await sut.Execute(CreateActorContext(), queryB, includeMargin: false, includeSellerBreakdown: false);

        Assert.True(resultA.IsSuccess && resultB.IsSuccess);
        Assert.Equal(1, resultA.Value!.Totals.SalesCount);
        Assert.Equal(1, resultA.Value.Totals.ReturnsCount);
        Assert.Equal(50m, resultA.Value.Totals.ReturnsAmount);
        Assert.Equal(50m, resultA.Value.Totals.NetSalesAmount);

        // The return does not belong to the cashier who processed it.
        Assert.Equal(0, resultB.Value!.Totals.SalesCount);
        Assert.Equal(0, resultB.Value.Totals.ReturnsCount);
        Assert.Equal(0m, resultB.Value.Totals.NetSalesAmount);
    }

    [Fact]
    public async Task Execute_ShouldAttributeReturnsInSellerBreakdown()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var sellerB = Guid.NewGuid();
        var saleA = BuildSale(SaleType.Sale, UserId, Day.AddHours(10), 100m, Registered(50m, 2, 20m));
        dbContext.Sales.Add(saleA);
        dbContext.Sales.Add(BuildSale(SaleType.Return, sellerB, Day.AddHours(11), -50m,
            Registered(50m, -1, 20m), soldByName: "Seller B", originalSaleId: saleA.Id));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var result = await sut.Execute(CreateActorContext(), Range(Day), includeMargin: false, includeSellerBreakdown: true);

        Assert.True(result.IsSuccess);
        var seller = Assert.Single(result.Value!.BySeller);
        Assert.Equal(UserId, seller.Id);
        Assert.Equal(1, seller.SalesCount);
        Assert.Equal(50m, seller.NetSalesAmount); // 100 - 50 return attributed to A
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenDateRangeIsInverted()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var sut = new GetSalesStats(dbContext);

        var query = new SalesStatsQueryDto
        {
            DateFrom = Day.AddDays(1),
            DateTo = Day
        };

        var result = await sut.Execute(CreateActorContext(), query, includeMargin: false, includeSellerBreakdown: false);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetSalesStatsErrors.InvalidDateRange, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldBucketByLocalDay_UsingTzOffset()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        // 02:00 UTC on Nov 6 is 22:00 on Nov 5 in UTC-4.
        dbContext.Sales.Add(BuildSale(SaleType.Sale, UserId, new DateTime(2026, 11, 6, 2, 0, 0, DateTimeKind.Utc),
            100m, Registered(50m, 2, 20m)));
        await dbContext.SaveChangesAsync();

        var sut = new GetSalesStats(dbContext);

        var localQuery = new SalesStatsQueryDto
        {
            DateFrom = Day,
            DateTo = Day.AddDays(1),
            TzOffsetMinutes = -240
        };

        var utcQuery = new SalesStatsQueryDto
        {
            DateFrom = Day,
            DateTo = Day.AddDays(1),
            TzOffsetMinutes = 0
        };

        var local = await sut.Execute(CreateActorContext(), localQuery, false, false);
        var utc = await sut.Execute(CreateActorContext(), utcQuery, false, false);

        Assert.True(local.IsSuccess && utc.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 5), Assert.Single(local.Value!.ByDay).Date);
        Assert.Equal(new DateOnly(2026, 11, 6), Assert.Single(utc.Value!.ByDay).Date);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static SalesStatsQueryDto Range(DateTime day) => new()
    {
        DateFrom = day,
        DateTo = day
    };

    private static (Guid? VariantId, decimal Price, int Qty, decimal? Cost, string Name) Registered(
        decimal price, int qty, decimal? cost, string name = "Test Product", Guid? variantId = null)
        => (variantId ?? Guid.NewGuid(), price, qty, cost, name);

    private static (Guid? VariantId, decimal Price, int Qty, decimal? Cost, string Name) Free(
        decimal price, int qty, string name)
        => (null, price, qty, null, name);

    private static Sale BuildSale(
        SaleType type,
        Guid soldById,
        DateTime createdAt,
        decimal total,
        (Guid? VariantId, decimal Price, int Qty, decimal? Cost, string Name) item,
        string soldByName = "Test User",
        Guid? originalSaleId = null)
    {
        var saleId = Guid.NewGuid();
        var sale = new Sale
        {
            Id = saleId,
            Number = 1,
            BranchId = BranchId,
            SoldById = soldById,
            SoldByName = soldByName,
            CreatedBy = soldById,
            CreatedByName = soldByName,
            CashRegisterClosureId = CashClosureId,
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Type = type,
            OriginalSaleId = originalSaleId,
            TotalAmount = total,
            CreatedAt = createdAt,
            TenantId = TenantId
        };

        sale.SaleItems.Add(new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = saleId,
            ProductVariantId = item.VariantId,
            ProductSku = item.VariantId is null ? string.Empty : "SKU-001",
            ProductDisplayName = item.Name,
            Quantity = item.Qty,
            UnitPrice = item.Price,
            UnitCost = item.Cost,
            DiscountAmount = 0,
            FinalPrice = item.Price * item.Qty,
            CreatedAt = createdAt,
            TenantId = TenantId
        });

        return sale;
    }
}
