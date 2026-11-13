using Common.Contracts.authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Module.Inventory.Application.UseCases.Receptions;
using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Application.UseCases.Receptions.Edit;
using Module.Inventory.Domain.Organization;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Receptions;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class DraftReceptionEditTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid OtherBranchId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProviderId = Guid.NewGuid();
    private static readonly Guid VariantA = Guid.NewGuid();
    private static readonly Guid VariantB = Guid.NewGuid();

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

    private static ActorContext CreateActorContext(Guid? branchId = null)
        => new(TenantId, UserId, "Test User", branchId ?? BranchId, [branchId ?? BranchId]);

    private static async Task SeedCatalog(TestAppDbContext ctx)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Name = "Nike", Prefix = "NIK", CreatedBy = TenantId, CreatedByName = "Test User" };
        var category = new Category { Id = Guid.NewGuid(), Name = "Zapatillas", CreatedBy = TenantId, CreatedByName = "Test User" };
        var color = new Color { Id = Guid.NewGuid(), Name = "Negro", CreatedBy = TenantId, CreatedByName = "Test User" };
        var size = new Size { Id = Guid.NewGuid(), Name = "42", SortOrder = 1, CreatedBy = TenantId, CreatedByName = "Test User" };
        ctx.Brands.Add(brand);
        ctx.Categories.Add(category);
        ctx.Colors.Add(color);
        ctx.Sizes.Add(size);

        var provider = Provider.Create("Proveedor", TenantId, UserId, "Test User");
        provider.Id = ProviderId;
        ctx.Providers.Add(provider);

        var product = Product.Create("Air Max", "d", category.Id, brand.Id, Gender.Unisex, "NIK-1", TenantId, UserId, "Test User");
        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();

        var variantA = ProductVariant.Create(product.Id, color.Id, size.Id, 100m, "NIK-1-001", TenantId, UserId, "Test User");
        variantA.Id = VariantA;
        ctx.ProductVariants.Add(variantA);

        var variantB = ProductVariant.Create(product.Id, color.Id, size.Id, 100m, "NIK-1-002", TenantId, UserId, "Test User");
        variantB.Id = VariantB;
        ctx.ProductVariants.Add(variantB);

        await ctx.SaveChangesAsync();
    }

    private static CreateReceptionUc CreateCreateSut(TestAppDbContext ctx)
        => new(ctx, new FakeInventoryNumberGenerator(), NullLogger<CreateReceptionUc>.Instance);

    private static async Task<Guid> CreateDraftAsync(TestAppDbContext ctx, ActorContext? actor = null)
    {
        var result = await CreateCreateSut(ctx).Execute(
            actor ?? CreateActorContext(),
            new CreateStockReceptionDto
            {
                ProviderId = ProviderId,
                SaveAsDraft = true,
                Items = [new CreateStockReceptionItemDto { ProductVariantId = VariantA, QuantityReceived = 3, UnitCost = 0 }]
            });

        Assert.True(result.IsSuccess, $"Expected draft creation success but got: {result.Error?.Code} - {result.Error?.Message}");
        return result.Value!.Id;
    }

    private static async Task ForceStatus(TestAppDbContext ctx, Guid id, ReceptionStatus status)
    {
        var reception = await ctx.StockReceptions.Include(r => r.Items).SingleAsync(r => r.Id == id);
        reception.Status = status;
        await ctx.SaveChangesAsync();
    }

    // --- Add items ---

    [Fact]
    public async Task AddItems_ShouldAppendNewVariant()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx);

        var sut = new AddItemsToDraftUc(ctx, NullLogger<AddItemsToDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(), draftId, new AddDraftItemsDto
        {
            Items = [new DraftItemDto { ProductVariantId = VariantB, QuantityReceived = 5, UnitCost = 12m }]
        });

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Contains(result.Value.Items, i => i.ProductVariantId == VariantB && i.QuantityReceived == 5 && i.UnitCost == 12m);
    }

    [Fact]
    public async Task AddItems_ShouldSumQuantity_ForExistingVariant_AndRefreshCostOnlyWhenPositive()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx); // VariantA: 3 uds @ 0

        var sut = new AddItemsToDraftUc(ctx, NullLogger<AddItemsToDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(), draftId, new AddDraftItemsDto
        {
            Items = [new DraftItemDto { ProductVariantId = VariantA, QuantityReceived = 2, UnitCost = 10m }]
        });

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(5, item.QuantityReceived); // 3 + 2
        Assert.Equal(10m, item.UnitCost);       // positive cost replaces

        // Adding again with 0 cost keeps the previous one.
        var second = await sut.Execute(CreateActorContext(), draftId, new AddDraftItemsDto
        {
            Items = [new DraftItemDto { ProductVariantId = VariantA, QuantityReceived = 1, UnitCost = 0 }]
        });

        Assert.True(second.IsSuccess);
        item = Assert.Single(second.Value!.Items);
        Assert.Equal(6, item.QuantityReceived);
        Assert.Equal(10m, item.UnitCost); // 0 does not overwrite
    }

    [Fact]
    public async Task AddItems_ShouldReturnError_WhenNotDraft()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx);
        await ForceStatus(ctx, draftId, ReceptionStatus.Confirmed);

        var sut = new AddItemsToDraftUc(ctx, NullLogger<AddItemsToDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(), draftId, new AddDraftItemsDto
        {
            Items = [new DraftItemDto { ProductVariantId = VariantB, QuantityReceived = 1, UnitCost = 5m }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(EditDraftErrors.NotDraft, result.Error);
    }

    [Fact]
    public async Task AddItems_ShouldReturnError_WhenReceptionBelongsToOtherBranch()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx);

        var sut = new AddItemsToDraftUc(ctx, NullLogger<AddItemsToDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(OtherBranchId), draftId, new AddDraftItemsDto
        {
            Items = [new DraftItemDto { ProductVariantId = VariantB, QuantityReceived = 1, UnitCost = 5m }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(EditDraftErrors.ReceptionNotFound, result.Error);
    }

    // --- Full edit ---

    [Fact]
    public async Task UpdateDraft_ShouldReplaceItems_AndChangeProviderAndNotes()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx);

        var sut = new UpdateDraftUc(ctx, NullLogger<UpdateDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(), draftId, new UpdateDraftDto
        {
            ProviderId = ProviderId,
            Notes = "Pedido actualizado",
            Items =
            [
                new DraftItemDto { ProductVariantId = VariantB, QuantityReceived = 4, UnitCost = 8m }
            ]
        });

        Assert.True(result.IsSuccess, $"Expected success but got: {result.Error?.Code} - {result.Error?.Message}");
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(VariantB, item.ProductVariantId); // VariantA removed
        Assert.Equal(4, item.QuantityReceived);
        Assert.Equal("Pedido actualizado", result.Value.Notes);

        // Number is not re-issued on edits.
        var saved = await ctx.StockReceptions.Include(r => r.Items).SingleAsync(r => r.Id == draftId);
        var originalNumber = saved.Number;
        Assert.Equal(1, saved.Items.Count);

        var second = await sut.Execute(CreateActorContext(), draftId, new UpdateDraftDto
        {
            ProviderId = ProviderId,
            Notes = "Segunda edicion",
            Items = [new DraftItemDto { ProductVariantId = VariantA, QuantityReceived = 1, UnitCost = 2m }]
        });

        Assert.True(second.IsSuccess);
        Assert.Equal(originalNumber, second.Value!.Number);
    }

    [Fact]
    public async Task UpdateDraft_ShouldReturnError_WhenNotDraft()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx);
        await ForceStatus(ctx, draftId, ReceptionStatus.Reverted);

        var sut = new UpdateDraftUc(ctx, NullLogger<UpdateDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(), draftId, new UpdateDraftDto
        {
            ProviderId = ProviderId,
            Items = [new DraftItemDto { ProductVariantId = VariantA, QuantityReceived = 1, UnitCost = 2m }]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(EditDraftErrors.NotDraft, result.Error);
    }

    // --- Delete ---

    [Fact]
    public async Task DeleteDraft_ShouldRemoveReception_AndItems()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx);

        var sut = new DeleteDraftUc(ctx, NullLogger<DeleteDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(), draftId);

        Assert.True(result.IsSuccess);
        Assert.False(await ctx.StockReceptions.AnyAsync(r => r.Id == draftId));
        Assert.False(await ctx.Set<StockReceptionItem>().AnyAsync(i => i.StockReceptionId == draftId));
    }

    [Fact]
    public async Task DeleteDraft_ShouldReturnError_WhenNotDraft()
    {
        using var ctx = CreateDbContext();
        await SeedCatalog(ctx);
        var draftId = await CreateDraftAsync(ctx);
        await ForceStatus(ctx, draftId, ReceptionStatus.Confirmed);

        var sut = new DeleteDraftUc(ctx, NullLogger<DeleteDraftUc>.Instance);

        var result = await sut.Execute(CreateActorContext(), draftId);

        Assert.False(result.IsSuccess);
        Assert.Equal(EditDraftErrors.NotDraft, result.Error);
        Assert.True(await ctx.StockReceptions.AnyAsync(r => r.Id == draftId));
    }
}
