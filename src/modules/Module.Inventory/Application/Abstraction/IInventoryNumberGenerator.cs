using Common.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Module.Inventory.Application.Abstraction;

public interface IInventoryNumberGenerator
{
    Task<int> NextAsync(DatabaseFacade db, Guid tenantId, InventoryCounterKey key, CancellationToken cancellationToken = default);
}