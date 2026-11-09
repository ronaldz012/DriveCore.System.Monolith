using Common.Contracts.authentication;
using Module.Sales.Application.Abstraction;
using Module.Sales.Application.UseCases.Sales.Get;
using Module.Sales.Domain;

namespace Test.Sales;

public class ListSellersTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid OtherBranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CashClosureId = Guid.NewGuid();

    [Fact]
    public async Task Execute_ShouldListDistinctSellers_FromBranchSales_WithLatestName()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var anaId = Guid.NewGuid();
        var betoId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        AddSale(dbContext, anaId, "Ana Old", new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        AddSale(dbContext, anaId, "Ana New", new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc));
        AddSale(dbContext, betoId, "Beto", new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc));
        // Another branch must not leak into the list.
        AddSale(dbContext, carlaId, "Carla", new DateTime(2026, 2, 3, 10, 0, 0, DateTimeKind.Utc), OtherBranchId);
        await dbContext.SaveChangesAsync();

        var sut = new ListSellers(dbContext);

        var result = await sut.Execute(CreateActorContext());

        Assert.True(result.IsSuccess);
        var sellers = result.Value!;
        Assert.Equal(2, sellers.Count);
        Assert.Equal("Ana New", sellers.Single(s => s.Id == anaId).Name);
        Assert.Equal("Beto", sellers.Single(s => s.Id == betoId).Name);
        Assert.DoesNotContain(sellers, s => s.Id == carlaId);
        // Ordered by name.
        Assert.Equal(["Ana New", "Beto"], sellers.Select(s => s.Name).ToArray());
    }

    [Fact]
    public async Task Execute_ShouldReturnEmpty_WhenBranchHasNoSales()
    {
        var tenantCtx = TestSalesDbContextFactory.CreateTenantContext(TenantId);
        using var dbContext = TestSalesDbContextFactory.Create(tenantCtx);

        var sut = new ListSellers(dbContext);

        var result = await sut.Execute(CreateActorContext());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    private static ActorContext CreateActorContext()
        => new(TenantId, UserId, "Test User", BranchId, [BranchId]);

    private static void AddSale(
        ISalesDbContext ctx,
        Guid soldById,
        string soldByName,
        DateTime createdAt,
        Guid? branchId = null)
    {
        ctx.Sales.Add(new Sale
        {
            Id = Guid.NewGuid(),
            Number = 1,
            BranchId = branchId ?? BranchId,
            SoldById = soldById,
            SoldByName = soldByName,
            CreatedBy = soldById,
            CreatedByName = soldByName,
            CashRegisterClosureId = CashClosureId,
            PaymentMethod = PaymentMethod.Cash,
            DocumentType = DocumentType.Ticket,
            Type = SaleType.Sale,
            TotalAmount = 100m,
            CreatedAt = createdAt,
            TenantId = TenantId
        });
    }
}
