using Common.Contracts.authentication;
using Common.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Module.Sales.Application.Abstraction;
using Npgsql;

namespace Module.Sales.Infrastructure.Services;

public class SalesNumberGenerator(ITenantConnectionContext tenantContext) : ISalesNumberGenerator
{
    public async Task<int> NextAsync(DatabaseFacade db, Guid tenantId, SalesCounterKey key, CancellationToken cancellationToken = default)
    {
        if (db.CurrentTransaction == null)
            throw new InvalidOperationException(
                $"{nameof(SalesNumberGenerator)} requiere una transaccion abierta en el contexto para reservar el correlativo '{key}'. Sin ella el SQL corre en autocommit y el numero se consumiria aunque el guardado posterior falle.");

        var counterKey = key.ToName();

        var sql = $"""
                   INSERT INTO "{tenantContext.Schema}"."DocumentCounters" ("TenantId","CounterKey","LastNumber")
                   VALUES (@tenantId, @key, 1)
                   ON CONFLICT ("TenantId","CounterKey")
                   DO UPDATE SET "LastNumber" = "DocumentCounters"."LastNumber" + 1
                   RETURNING "LastNumber"
                   """;

        var result = await db
            .SqlQueryRaw<int>(sql, new NpgsqlParameter("tenantId", tenantId), new NpgsqlParameter("key", counterKey))
            .ToListAsync(cancellationToken);

        return result[0];
    }
}