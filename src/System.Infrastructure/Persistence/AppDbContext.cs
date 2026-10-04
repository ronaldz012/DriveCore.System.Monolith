using Common.Contracts.authentication;
using Common.Domain;
using Common.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Organization;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Receptions;
using Module.Inventory.Domain.Transfers;
using Module.Inventory.Infrastructure.Persistence;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;
using Module.Sales.Infrastructure.Persistence;

namespace System.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, ITenantConnectionContext tenantConnectionContext)
    : DbContext(options), ISalesDbContext, IInvDbContext
{
    // ===== Sales =====
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }
    public DbSet<CashRegisterClosure> CashRegisterClosures { get; set; }
    public DbSet<CashRegisterMovement> CashRegisterMovements { get; set; }

    // ===== Inventory =====
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<BranchInventory> BranchInventories { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Provider> Providers { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Color> Colors { get; set; }
    public DbSet<Size> Sizes { get; set; }
    public DbSet<StockReception> StockReceptions { get; set; }
    public DbSet<StockReceptionItem> StockReceptionItems { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<StockTransfer> StockTransfers { get; set; }
    public DbSet<StockTransferItem> StockTransferItems { get; set; }
    public DbSet<DocumentCounter> DocumentCounters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SalesEntityConfiguration.Apply(modelBuilder);
        InventoryEntityConfiguration.Apply(modelBuilder);
        DocumentCounterConfiguration.Apply(modelBuilder);

        // Filtros por tenant (capturan this.tenantConnectionContext -> evaluado por DbContext actual)
        modelBuilder.Entity<Product>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId && x.DeletedAt == null);
        modelBuilder.Entity<ProductVariant>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId && x.DeletedAt == null);
        modelBuilder.Entity<BranchInventory>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<Category>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<Provider>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<Brand>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<Color>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<Size>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<StockReception>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<StockReceptionItem>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<StockMovement>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<StockTransfer>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<StockTransferItem>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId);
        modelBuilder.Entity<Sale>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId && x.DeletedAt == null);
        modelBuilder.Entity<SaleItem>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId && x.DeletedAt == null);
        modelBuilder.Entity<CashRegisterClosure>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId && x.DeletedAt == null);
        modelBuilder.Entity<CashRegisterMovement>().HasQueryFilter(x => x.TenantId == tenantConnectionContext.TenantId && x.DeletedAt == null);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantConnectionContext.TenantId
            ?? throw new InvalidOperationException("Tenant is not set");

        var entries = ChangeTracker.Entries<IMustHaveTenant>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Entity.TenantId = currentTenantId;
            else if (entry.State == EntityState.Modified)
                entry.Property(x => x.TenantId).IsModified = false;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
