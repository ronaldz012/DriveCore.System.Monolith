using Common.Contracts.authentication;
using Common.Contracts.branches;
using Common.Contracts.branches.dtos;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.UseCases.Products.GetById;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Inventory;
using Moq;
using System.Infrastructure.Persistence;

namespace Test.Inventory;

public class ProductDetailsTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid BranchA = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static ActorContext CreateActorContext() =>
        new(TenantId, UserId, "Test User", BranchA, [BranchA, BranchB]);

    private static IBranchService CreateBranchServiceMock()
    {
        var mock = new Mock<IBranchService>();
        mock.Setup(s => s.GetBranchesByIds(It.IsAny<List<Guid>>()))
            .ReturnsAsync((List<Guid> ids) => ids.Select(id => new BranchDto
            {
                Id = id,
                Name = id == BranchA ? "Sucursal A" : "Sucursal B",
                BranchCode = "Loc",
                Status = true
            }).ToList());
        return mock.Object;
    }

    private static AppDbContext CreateDbContext()
    {
        var tenantCtx = TestInvDbContextFactory.CreateTenantContext(TenantId);
        return TestInvDbContextFactory.Create(tenantCtx);
    }

    [Fact]
    public async Task Execute_ShouldReturnZeroForMissingBranchInventory()
    {
        using var ctx = CreateDbContext();
        SeedProductWithSingleBranchInventory(ctx, BranchA, 5);

        var sut = new ProductDetails(ctx, CreateBranchServiceMock());

        var result = await sut.Execute(CreateActorContext(), ctx.Products.First().Id);

        Assert.True(result.IsSuccess);
        var variant = Assert.Single(result.Value.Variants);
        Assert.Equal(2, variant.BranchStocks.Count);

        var stockA = variant.BranchStocks.First(b => b.BranchId == BranchA);
        var stockB = variant.BranchStocks.First(b => b.BranchId == BranchB);

        Assert.Equal(5, stockA.Stock);
        Assert.Equal("Sucursal A", stockA.BranchName);
        Assert.Equal(0, stockB.Stock);
        Assert.Equal("Sucursal B", stockB.BranchName);
        Assert.Equal(5, variant.TotalAvailable);
        Assert.Equal(5, result.Value.TotalAvailable);
    }

    [Fact]
    public async Task Execute_ShouldReturnZeroForAllBranches_WhenNoInventoryAtAll()
    {
        using var ctx = CreateDbContext();
        SeedProductWithNoInventory(ctx);

        var sut = new ProductDetails(ctx, CreateBranchServiceMock());

        var result = await sut.Execute(CreateActorContext(), ctx.Products.First().Id);

        Assert.True(result.IsSuccess);
        var variant = Assert.Single(result.Value.Variants);
        Assert.Equal(2, variant.BranchStocks.Count);
        Assert.All(variant.BranchStocks, bs => Assert.Equal(0, bs.Stock));
        Assert.Equal(0, variant.TotalAvailable);
    }

    private static void SeedProductWithSingleBranchInventory(AppDbContext ctx, Guid branchId, int stock)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Name = "Brand", TenantId = TenantId, CreatedAt = DateTime.UtcNow };
        var category = new Category { Id = Guid.NewGuid(), Name = "Cat", TenantId = TenantId, CreatedAt = DateTime.UtcNow };
        var color = new Color { Id = Guid.NewGuid(), Name = "Red", TenantId = TenantId };
        var size = new Size { Id = Guid.NewGuid(), Name = "M", TenantId = TenantId };
        ctx.Brands.Add(brand);
        ctx.Categories.Add(category);
        ctx.Colors.Add(color);
        ctx.Sizes.Add(size);
        ctx.SaveChanges();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "Test Product",
            InternalCode = "IC-001",
            BrandId = brand.Id,
            CategoryId = category.Id,
            BasePrice = 100m,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Products.Add(product);
        ctx.SaveChanges();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            ProductId = product.Id,
            Sku = "SKU-001",
            ColorId = color.Id,
            SizeId = size.Id,
            Price = 100m,
            AverageCost = 50m
        };
        ctx.ProductVariants.Add(variant);
        ctx.SaveChanges();

        ctx.BranchInventories.Add(new BranchInventory
        {
            BranchId = branchId,
            TenantId = TenantId,
            ProductVariantId = variant.Id,
            Stock = stock
        });
        ctx.SaveChanges();
    }

    private static void SeedProductWithNoInventory(AppDbContext ctx)
    {
        var brand = new Brand { Id = Guid.NewGuid(), Name = "Brand", TenantId = TenantId, CreatedAt = DateTime.UtcNow };
        var category = new Category { Id = Guid.NewGuid(), Name = "Cat", TenantId = TenantId, CreatedAt = DateTime.UtcNow };
        var color = new Color { Id = Guid.NewGuid(), Name = "Red", TenantId = TenantId };
        var size = new Size { Id = Guid.NewGuid(), Name = "M", TenantId = TenantId };
        ctx.Brands.Add(brand);
        ctx.Categories.Add(category);
        ctx.Colors.Add(color);
        ctx.Sizes.Add(size);
        ctx.SaveChanges();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "Test Product",
            InternalCode = "IC-001",
            BrandId = brand.Id,
            CategoryId = category.Id,
            BasePrice = 100m,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Products.Add(product);
        ctx.SaveChanges();

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            ProductId = product.Id,
            Sku = "SKU-001",
            ColorId = color.Id,
            SizeId = size.Id,
            Price = 100m,
            AverageCost = 50m
        };
        ctx.ProductVariants.Add(variant);
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Execute_ShouldOrderVariantsByColorThenSize_WhenSkusAreOutOfOrder()
    {
        using var ctx = CreateDbContext();

        var brand = new Brand { Id = Guid.NewGuid(), Name = "Brand", TenantId = TenantId, CreatedAt = DateTime.UtcNow };
        var category = new Category { Id = Guid.NewGuid(), Name = "Cat", TenantId = TenantId, CreatedAt = DateTime.UtcNow };
        var azul = new Color { Id = Guid.NewGuid(), Name = "azul", TenantId = TenantId };
        var negro = new Color { Id = Guid.NewGuid(), Name = "negro", TenantId = TenantId };
        var size38 = new Size { Id = Guid.NewGuid(), Name = "38", SortOrder = 1, TenantId = TenantId };
        var size41 = new Size { Id = Guid.NewGuid(), Name = "41", SortOrder = 2, TenantId = TenantId };
        var size42 = new Size { Id = Guid.NewGuid(), Name = "42", SortOrder = 3, TenantId = TenantId };
        ctx.Brands.Add(brand);
        ctx.Categories.Add(category);
        ctx.Colors.AddRange(azul, negro);
        ctx.Sizes.AddRange(size38, size41, size42);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = "Test Product",
            InternalCode = "IC-001",
            BrandId = brand.Id,
            CategoryId = category.Id,
            BasePrice = 100m,
            CreatedAt = DateTime.UtcNow
        };
        ctx.Products.Add(product);

        // Creadas desordenadas y con SKUs que no reflejan el orden lógico
        // (azul 38 agregada al final, como variante tardía con SKU de cola)
        ctx.ProductVariants.AddRange(
            new ProductVariant { Id = Guid.NewGuid(), TenantId = TenantId, ProductId = product.Id, Sku = "SKU-001", ColorId = azul.Id, SizeId = size42.Id, Price = 100m },
            new ProductVariant { Id = Guid.NewGuid(), TenantId = TenantId, ProductId = product.Id, Sku = "SKU-002", ColorId = negro.Id, SizeId = size41.Id, Price = 100m },
            new ProductVariant { Id = Guid.NewGuid(), TenantId = TenantId, ProductId = product.Id, Sku = "SKU-003", ColorId = negro.Id, SizeId = size42.Id, Price = 100m },
            new ProductVariant { Id = Guid.NewGuid(), TenantId = TenantId, ProductId = product.Id, Sku = "SKU-004", ColorId = azul.Id, SizeId = size41.Id, Price = 100m },
            new ProductVariant { Id = Guid.NewGuid(), TenantId = TenantId, ProductId = product.Id, Sku = "SKU-005", ColorId = azul.Id, SizeId = size38.Id, Price = 100m });
        ctx.SaveChanges();

        var sut = new ProductDetails(ctx, CreateBranchServiceMock());

        var result = await sut.Execute(CreateActorContext(), product.Id);

        Assert.True(result.IsSuccess);
        var ordered = result.Value.Variants.Select(v => (v.Color, v.Size, v.Sku)).ToList();
        Assert.Equal(
        [
            ("azul", "38", "SKU-005"),
            ("azul", "41", "SKU-004"),
            ("azul", "42", "SKU-001"),
            ("negro", "41", "SKU-002"),
            ("negro", "42", "SKU-003")
        ], ordered);
    }
}
