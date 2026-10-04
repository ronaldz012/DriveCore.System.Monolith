using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Create;
using Module.Sales.Domain;
using Moq;

namespace Test.Sales;

public class CreateSaleTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid VariantId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();

    [Fact]
    public async Task Execute_ShouldReturnError_WhenProductsNotFound()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOpenCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), BranchId))
            .ReturnsAsync(new Error(ErrorCode.NotFound, "One or more products do not exist."));

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Items = [new CreateSaleItemDto { ProductVariantId = VariantId, Quantity = 1 }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCode.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenNoOpenCashRegister()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), BranchId))
            .ReturnsAsync(new List<ProductVariantStockDto>
            {
                new(VariantId, "SKU-001", "Test Product - Negro / 42", 100m, 10, true, 30m)
            });

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Items = [new CreateSaleItemDto { ProductVariantId = VariantId, Quantity = 1 }]
        });

        Assert.Equal(CreateSaleErrors.NoOpenCashRegister, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenCashClosureIsClosed()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedClosedCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), BranchId))
            .ReturnsAsync(new List<ProductVariantStockDto>
            {
                new(VariantId, "SKU-001", "Test Product - Negro / 42", 100m, 10, true, 30m)
            });

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Items = [new CreateSaleItemDto { ProductVariantId = VariantId, Quantity = 1 }]
        });

        Assert.Equal(CreateSaleErrors.NoOpenCashRegister, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenDeductStockFails()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOpenCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), BranchId))
            .ReturnsAsync(new List<ProductVariantStockDto>
            {
                new(VariantId, "SKU-001", "Test Product - Negro / 42", 100m, 10, true, 30m)
            });
        var deductError = new Error(ErrorCode.InvalidState, "Insufficient stock for product SKU-001.");
        inventoryMock
            .Setup(s => s.DeductStock(It.IsAny<List<StockDeductionDto>>(), BranchId, UserId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync(deductError);

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Items = [new CreateSaleItemDto { ProductVariantId = VariantId, Quantity = 1 }]
        });

        Assert.Equal(deductError, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenProductInactive()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOpenCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), BranchId))
            .ReturnsAsync(new List<ProductVariantStockDto>
            {
                new(VariantId, "SKU-001", "Test Product - Negro / 42", 100m, 10, false, 30m)
            });

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Items = [new CreateSaleItemDto { ProductVariantId = VariantId, Quantity = 1 }]
        });

        Assert.Equal(CreateSaleErrors.ProductInactive, result.Error);
    }

    [Fact]
    public async Task Execute_ShouldCreateSale_WhenValidRequest()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);
        SeedOpenCashClosure(dbContext);

        var inventoryMock = new Mock<IInventoryIntegrationService>();
        inventoryMock
            .Setup(s => s.GetVariantsWithStock(It.IsAny<List<Guid>>(), BranchId))
            .ReturnsAsync(new List<ProductVariantStockDto>
            {
                new(VariantId, "SKU-001", "Test Product - Negro / 42", 100m, 10, true, 30m)
            });
        inventoryMock
            .Setup(s => s.DeductStock(It.IsAny<List<StockDeductionDto>>(), BranchId, UserId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        var sut = CreateSut(dbContext, inventoryMock.Object);

        var result = await sut.Execute(CreateActorContext(), new CreateSaleDto
        {
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            TransactionCode = null,
            Notes = "Test sale",
            Items = [new CreateSaleItemDto { ProductVariantId = VariantId, Quantity = 2, DiscountAmount = 0 }]
        });

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");

        var saved = await dbContext.Sales.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(BranchId, saved.BranchId);
        Assert.Equal(UserId, saved.SoldById);
        Assert.Equal(CashClosureId, saved.CashRegisterClosureId);
        Assert.Equal(200m, saved.TotalAmount); // 100 * 2
        Assert.Single(saved.SaleItems);
        Assert.Equal(TenantId, saved.TenantId);
        Assert.Equal("Test User", saved.SoldByName);
        var savedItem = saved.SaleItems.First();
        Assert.Equal("SKU-001", savedItem.ProductSku);
        Assert.Equal("Test Product - Negro / 42", savedItem.ProductDisplayName);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static CreateSale CreateSut(
        ISalesDbContext dbContext,
        IInventoryIntegrationService? inventoryService = null)
    {
        return new CreateSale(
            dbContext,
            inventoryService ?? new Mock<IInventoryIntegrationService>().Object,
            new FakeSalesNumberGenerator(),
            new Mock<ILogger<CreateSale>>().Object);
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

    private static void SeedClosedCashClosure(ISalesDbContext ctx)
    {
        ctx.CashRegisterClosures.Add(new CashRegisterClosure
        {
            Id = CashClosureId,
            BranchId = BranchId,
            IsOpen = false,
            OpenAt = DateTime.UtcNow.AddHours(-8),
            ClosedAt = DateTime.UtcNow,
            OpeningBalance = 500m,
        });
        ctx.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
