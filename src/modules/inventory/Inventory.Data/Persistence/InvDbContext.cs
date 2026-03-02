using Inventory.Data.Entities.Inventory;
using Inventory.Data.Entities.Organization;
using Inventory.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Data.Persistence;

public class InvDbContext(DbContextOptions<InvDbContext> options) : DbContext(options)
{
    public DbSet<Product>  Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<BranchInventory> BranchInventories { get; set; }
    
    public DbSet<Category>  Categories { get; set; }
    public DbSet<Provider>  Providers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>(entity =>
            {
                entity.HasMany(product => product.ProductVariants)
                    .WithOne(variant => variant.Product)
                    .HasForeignKey(variant => variant.ProductId);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId);
                
            }
        );
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasMany(pv => pv.BranchInventories)
                .WithOne(inv => inv.ProductVariant)
                .HasForeignKey(inv => inv.ProductVariantId);
        });
        
    }
}