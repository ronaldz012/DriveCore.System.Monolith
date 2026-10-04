using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Products;
using Module.Inventory.Infrastructure;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Return;
using Module.Sales.Domain;
using Moq;
using System.Infrastructure.Persistence;

namespace Test.Sales;

public class CreateReturnTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();
    private static readonly Guid OriginalSaleId = Guid.NewGuid();
    private static readonly Guid OriginalSaleItemId = Guid.NewGuid();

    [Fact]
    public async Task Execute_ShouldReturnError_WhenOriginalSaleNotFound()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var sut = CreateSut(dbContext);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.OriginalSaleNotFound, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenSaleIsNotSaleType()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Return);

        var sut = CreateSut(dbContext);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.OriginalSaleNotEligible, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenAlreadyReturned()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);
        SeedExistingReturn(dbContext);

        var sut = CreateSut(dbContext);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.AlreadyReturned, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenItemNotFoundInOriginalSale()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);

        var sut = CreateSut(dbContext);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = Guid.NewGuid(), Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.OriginalItemNotFound, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenQuantityExceedsSoldQuantity()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);

        var sut = CreateSut(dbContext);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 10 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.ExceedsReturnableQuantity, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenNoOpenCashRegister()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);

        var sut = CreateSut(dbContext);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.NoOpenCashRegister, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenReturnStockFails()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);
        SeedOpenCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.ReturnStock(It.IsAny<List<StockReturnDto>>(), BranchId, UserId, It.IsAny<string>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(new Error(ErrorCode.NotFound, "Product variant not found."));

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task Execute_ShouldCreateReturn_WhenValidRequest()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);
        SeedOpenCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.ReturnStock(It.IsAny<List<StockReturnDto>>(), BranchId, UserId, It.IsAny<string>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(true);

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 2 }]
        });

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var saved = await dbContext.Sales.FirstOrDefaultAsync(s => s.Type == SaleType.Return);
        Assert.NotNull(saved);
        Assert.Equal(BranchId, saved.BranchId);
        Assert.Equal(UserId, saved.SoldById);
        Assert.Equal(OriginalSaleId, saved.OriginalSaleId);
        Assert.Equal(SaleType.Return, saved.Type);
        Assert.Equal(TenantId, saved.TenantId);
        Assert.Single(saved.SaleItems);
        Assert.Equal(-2, saved.SaleItems.First().Quantity);
    }

    [Fact]
    public async Task Execute_ShouldCreateReturn_WithAllNegatives()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);
        SeedOpenCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.ReturnStock(It.IsAny<List<StockReturnDto>>(), BranchId, UserId, It.IsAny<string>(), It.IsAny<Guid>(), false))
            .ReturnsAsync(true);

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 2 }]
        });

        Assert.True(result.IsSuccess);

        var saved = await dbContext.Sales.Include(s => s.SaleItems).FirstAsync(s => s.Type == SaleType.Return);
        Assert.True(saved.TotalAmount < 0, $"TotalAmount should be negative, was {saved.TotalAmount}");
        Assert.True(saved.SaleItems.All(si => si.Quantity < 0), "SaleItem Quantity should be negative");
        Assert.True(saved.SaleItems.All(si => si.FinalPrice < 0), "SaleItem FinalPrice should be negative");
        // UnitCost snapshot stays positive (cost history), UnitPrice stays positive
        Assert.True(saved.SaleItems.All(si => si.UnitCost > 0), "UnitCost should remain positive");
        Assert.True(saved.SaleItems.All(si => si.UnitPrice > 0), "UnitPrice should remain positive");
        // Sum checks
        Assert.Equal(-200m, saved.TotalAmount); // 2 * 100 = 200 -> -200
        Assert.Equal(-200m, saved.SaleItems.First().FinalPrice);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenSaleBelongsToDifferentTenant()
    {
        var otherTenantId = Guid.Parse("00000000-0000-0000-0000-000000000099");
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(otherTenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOriginalSale(dbContext, SaleType.Sale);

        var sut = CreateSut(dbContext);

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ReturnErrors.OriginalSaleNotFound, result.Error);
    }

    [Fact]
    public async Task Execute_WithRealInventory_ShouldCreateReturnMovement_WithStockBeforeAfter()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var ctx = TestSalesDbContextFactory.Create(tenantCtx);

        // Seed catalog + variant with stock 5
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
        variant.AverageCost = 30m;
        ctx.ProductVariants.Add(variant);
        await ctx.SaveChangesAsync();

        ctx.BranchInventories.Add(new BranchInventory
        {
            ProductVariantId = VariantId,
            BranchId = BranchId,
            Stock = 5,
            CreatedBy = UserId,
            CreatedByName = "Test User"
        });
        await ctx.SaveChangesAsync();

        // Seed original sale that sold 3 units
        SeedOriginalSale(ctx, SaleType.Sale);
        // Adjust sale item to use our variant's SKU/cost
        var originalSale = await ctx.Sales.Include(s => s.SaleItems).FirstAsync(s => s.Id == OriginalSaleId);
        originalSale.SaleItems.First().ProductVariantId = VariantId;
        originalSale.SaleItems.First().Quantity = 3;
        originalSale.SaleItems.First().UnitCost = 30m;
        originalSale.SaleItems.First().UnitPrice = 100m;
        originalSale.SaleItems.First().FinalPrice = 300m;
        await ctx.SaveChangesAsync();

        // Reduce stock to simulate sale (5 -> 2)
        var inv = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        inv.Stock = 2;
        await ctx.SaveChangesAsync();

        ctx.CashRegisterClosures.Add(new CashRegisterClosure
        {
            Id = CashClosureId,
            BranchId = BranchId,
            IsOpen = true,
            OpenAt = DateTime.UtcNow,
            OpeningBalance = 500m,
        });
        await ctx.SaveChangesAsync();

        var inventoryService = new InventoryIntegrationService(ctx);
        var sut = new CreateReturn(ctx, inventoryService, new FakeSalesNumberGenerator(), Mock.Of<ILogger<CreateReturn>>());

        var result = await sut.Execute(CreateActorContext(), new CreateReturnDto
        {
            OriginalSaleId = OriginalSaleId,
            Items = [new CreateReturnItemDto { OriginalSaleItemId = OriginalSaleItemId, Quantity = 2 }]
        });

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var movement = await ctx.StockMovements.SingleAsync(m => m.MovementType == MovementType.Return);
        Assert.Equal(2m, movement.Quantity);
        Assert.Equal(30m, movement.UnitCost);
        Assert.Equal(MovementType.Return, movement.MovementType);
        Assert.Equal(2, movement.StockBefore);
        Assert.Equal(4, movement.StockAfter);

        var updatedInv = await ctx.BranchInventories.SingleAsync(bi => bi.ProductVariantId == VariantId);
        Assert.Equal(4, updatedInv.Stock);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static CreateReturn CreateSut(
        ISalesDbContext dbContext,
        IInventoryIntegrationService? inventoryService = null)
    {
        return new CreateReturn(
            dbContext,
            inventoryService ?? new Mock<IInventoryIntegrationService>().Object,
            new FakeSalesNumberGenerator(),
            new Mock<ILogger<CreateReturn>>().Object);
    }

    private static void SeedOriginalSale(ISalesDbContext ctx, SaleType type)
    {
        ctx.Sales.Add(new Sale
        {
            Id = OriginalSaleId,
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Original Buyer",
            CreatedBy = UserId,
            CreatedByName = "Original Buyer",
            CashRegisterClosureId = CashClosureId,
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Type = type,
            TotalAmount = 100m,
            TenantId = TenantId,
            SaleItems =
            [
                new SaleItem
                {
                    Id = OriginalSaleItemId,
                    ProductVariantId = VariantId,
                    ProductSku = "SKU-001",
                    ProductDisplayName = "Test Product - Negro / 42",
                    Quantity = 5,
                    UnitPrice = 100m,
                    UnitCost = 30m,
                    DiscountAmount = 0,
                    FinalPrice = 500m,
                    TenantId = TenantId
                }
            ]
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static void SeedExistingReturn(ISalesDbContext ctx)
    {
        ctx.Sales.Add(new Sale
        {
            Id = Guid.NewGuid(),
            BranchId = BranchId,
            SoldById = UserId,
            SoldByName = "Original Buyer",
            CreatedBy = UserId,
            CreatedByName = "Original Buyer",
            CashRegisterClosureId = CashClosureId,
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Return,
            OriginalSaleId = OriginalSaleId,
            TotalAmount = -50m,
            TenantId = TenantId
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }

    private static void SeedOpenCashClosure(ISalesDbContext ctx)
    {
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
}
