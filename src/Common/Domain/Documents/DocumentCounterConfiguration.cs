using Microsoft.EntityFrameworkCore;

namespace Common.Domain.Documents;

public static class DocumentCounterConfiguration
{
    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<DocumentCounter>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.CounterKey });
            entity.Property(e => e.CounterKey).HasMaxLength(50).IsRequired();
        });
    }
}