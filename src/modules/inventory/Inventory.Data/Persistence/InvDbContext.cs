using Inventory.Data.Entities.Inventory;
using Inventory.Data.Entities.Organization;
using Inventory.Data.Entities.Products;
using Inventory.Data.Entities.Receptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Data.Persistence;

public class InvDbContext(DbContextOptions<InvDbContext> options) : DbContext(options)
{
    public DbSet<Product>  Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<BranchInventory> BranchInventories { get; set; }
    
    public DbSet<Category>  Categories { get; set; }
    public DbSet<Provider>  Providers { get; set; }
    public DbSet<Brand>  Brands { get; set; }
    public DbSet<StockReception>  StockReceptions { get; set; }
    public DbSet<StockReceptionItem>  StockReceptionItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //SEEDER
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasData(
                new Category { Id = 1, Name = "Sin categoria" }
            );
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasData(
                new Brand { Id = 1, Name = "Sin Marca" });
        });
        //
        modelBuilder.Entity<Product>(entity =>
            {
                entity.HasMany(product => product.ProductVariants)
                    .WithOne(variant => variant.Product)
                    .HasForeignKey(variant => variant.ProductId);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId);
                
                entity.Property(p => p.CategoryId)
                    .IsRequired()
                    .HasDefaultValue(1);

                entity.HasOne(p => p.Brand)
                    .WithMany(b => b.Products)
                    .HasForeignKey(p => p.BrandId);
                
                entity.Property(p => p.BrandId)
                    .IsRequired()
                    .HasDefaultValue(1);
            }
        );
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasMany(pv => pv.BranchInventories)
                .WithOne(inv => inv.ProductVariant)
                .HasForeignKey(inv => inv.ProductVariantId);
        });
        
        //RECEPTIONS
        modelBuilder.Entity<StockReception>(entity =>
            {
                    entity.HasMany(r => r.Items)
                        .WithOne(i => i.StockReception)
                        .HasForeignKey(i => i.StockReceptionId);
                    
            }
        );

        modelBuilder.Entity<StockReceptionItem>(entity =>
        {
            entity.HasOne(ri => ri.ProductVariant)
                .WithMany(pv => pv.StockReceptionItems)
                .HasForeignKey(pv => pv.StockReceptionId);
        });


    }
}