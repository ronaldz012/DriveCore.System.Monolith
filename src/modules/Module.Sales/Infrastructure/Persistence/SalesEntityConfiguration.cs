using Microsoft.EntityFrameworkCore;
using Module.Sales.Domain;

namespace Module.Sales.Infrastructure.Persistence;

public static class SalesEntityConfiguration
{
    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<Sale>(entity =>
        {
            entity.HasMany(s => s.SaleItems)
                .WithOne(i => i.Sale)
                .HasForeignKey(i => i.SaleId);

            entity.HasMany(s => s.Payments)
                .WithOne(p => p.Sale)
                .HasForeignKey(p => p.SaleId);


            entity.HasOne(s => s.OriginalSale)
                .WithMany(s => s.Returns)
                .HasForeignKey(s => s.OriginalSaleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TenantId, e.Number })
                .IsUnique();
        });
        builder.Entity<SalePayment>(entity =>
        {
            // One line per (Sale, Method): the database rejects duplicate tenders
            // no matter who writes them (API, SQL, seeds). TenantId goes first per
            // the multi-tenant index convention.
            entity.HasIndex(e => new { e.TenantId, e.SaleId, e.Method })
                .IsUnique();
        });
        builder.Entity<SaleItem>(entity =>
        {
            entity.HasOne(i => i.OriginalSaleItem)
                .WithMany(i => i.ChildReturns)
                .HasForeignKey(i => i.OriginalSaleItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<CashRegisterClosure>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.BranchId })
                .IsUnique()
                .HasFilter("\"IsOpen\" = true")
                .HasDatabaseName("IX_CashRegisterClosures_Tenant_Branch_OpenOnly");

            entity.HasIndex(e => new { e.TenantId, e.Number })
                .IsUnique();

            entity.HasMany(c => c.Movements)
                .WithOne(m => m.CashRegisterClosure)
                .HasForeignKey(m => m.CashRegisterClosureId);
            entity.HasMany(c => c.Sales)
                .WithOne(s => s.CashRegisterClosure)
                .HasForeignKey(s => s.CashRegisterClosureId);
        });
        builder.Entity<CashRegisterMovement>(entity =>
        {
        });
    }
}
