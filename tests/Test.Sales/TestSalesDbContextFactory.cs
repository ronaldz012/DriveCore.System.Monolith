using System.Data.Common;
using System.Transactions;
using Common.Contracts.authentication;
using Common.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Module.Sales.Application.Abstraction;
using System.Infrastructure.Persistence;

namespace Test.Sales;

public static class TestSalesDbContextFactory
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static AppDbContext Create(Guid? tenantId = null)
    {
        return Create(CreateTenantContext(tenantId));
    }

    public static AppDbContext Create(ITenantConnectionContext tenant, string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? $"SalesTest_{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, tenant);
    }

    public static TenantConnectionContext CreateTenantContext(Guid? tenantId = null)
    {
        return new TenantConnectionContext
        {
            TenantId = tenantId ?? DefaultTenantId,
            Schema = "test_schema",
            DatabaseName = "test_database",
        };
    }
}

public class TenantConnectionContext : ITenantConnectionContext
{
    public string? Schema { get; set; }
    public Guid? TenantId { get; set; }
    public string? DatabaseName { get; set; }
    public DbConnection Connection => throw new NotSupportedException("InMemory tests do not support Connection.");
    public Task EnsureOpenAsync() => Task.CompletedTask;
    public Task<TransactionScope> BeginTransactionScopeAsync() =>
        Task.FromResult(new TransactionScope(TransactionScopeOption.Suppress));
}

public sealed class FakeSalesNumberGenerator : ISalesNumberGenerator
{
    private int _last;

    public Task<int> NextAsync(DatabaseFacade db, Guid tenantId, SalesCounterKey key, CancellationToken cancellationToken = default)
        => Task.FromResult(++_last);
}