using Microsoft.EntityFrameworkCore;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Organization;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Receptions;
using Module.Inventory.Domain.Transfers;

namespace Module.Inventory.Infrastructure.Persistence;

public static class InventoryEntityConfiguration
{
    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.HasMany(product => product.ProductVariants)
                .WithOne(variant => variant.Product)
                .HasForeignKey(variant => variant.ProductId);
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);
            entity.HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId);
            entity.HasIndex(x => new { x.TenantId, x.CategoryId, x.BrandId, x.Name })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");
            entity.HasIndex(x => new { x.TenantId, x.InternalCode })
                .IsUnique();
        });
        builder.Entity<ProductVariant>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.ProductId, x.ColorId, x.SizeId })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");
            entity.HasMany(pv => pv.BranchInventories)
                .WithOne(inv => inv.ProductVariant)
                .HasForeignKey(inv => inv.ProductVariantId);
            entity.HasMany(pv => pv.StockMovements)
                .WithOne(inv => inv.ProductVariant)
                .HasForeignKey(inv => inv.ProductVariantId);
            entity.HasMany(pv => pv.TransferItems)
                .WithOne(ti => ti.ProductVariant)
                .HasForeignKey(ti => ti.ProductVariantId);
            entity.HasOne(pv => pv.Color)
                .WithMany(c => c.ProductVariant)
                .HasForeignKey(pv => pv.ColorId);
        });
        builder.Entity<BranchInventory>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.BranchId, x.ProductVariantId }).IsUnique();
        });
        builder.Entity<Category>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });
        builder.Entity<Provider>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasMany(p => p.StockReceptions)
                .WithOne(r => r.Provider)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Brand>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Prefix }).IsUnique();
        });
        builder.Entity<Color>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
        });
        builder.Entity<Size>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasMany(s => s.ProductVariants)
                .WithOne(pv => pv.Size)
                .HasForeignKey(pv => pv.SizeId);
        });
        builder.Entity<StockReception>(entity =>
        {
            entity.HasMany(r => r.Items)
                .WithOne(i => i.StockReception)
                .HasForeignKey(i => i.StockReceptionId);

            entity.HasIndex(e => new { e.TenantId, e.Number })
                .IsUnique();
        });
        builder.Entity<StockReceptionItem>(entity =>
        {
            entity.HasOne(ri => ri.ProductVariant)
                .WithMany(pv => pv.StockReceptionItems)
                .HasForeignKey(pv => pv.ProductVariantId);
        });
        builder.Entity<StockMovement>(entity =>
        {
        });
        builder.Entity<StockTransfer>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Number })
                .IsUnique();
        });
        builder.Entity<StockTransferItem>(entity =>
        {
        });
    }
}
