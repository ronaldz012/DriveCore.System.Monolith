using Common.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Module.Sales.Application.Abstraction;

public interface ISalesNumberGenerator
{
    Task<int> NextAsync(DatabaseFacade db, Guid tenantId, SalesCounterKey key, CancellationToken cancellationToken = default);
}