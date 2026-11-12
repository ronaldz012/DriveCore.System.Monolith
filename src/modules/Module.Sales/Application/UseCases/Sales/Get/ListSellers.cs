using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;

namespace Module.Sales.Application.UseCases.Sales.Get;

public class ListSellers(ISalesDbContext context)
{
    public async Task<Result<List<SellerDto>>> Execute(ActorContext ctx)
    {
        // Sellers are derived from the branch sales: every listed seller can actually
        // be filtered (no empty results) and there is no Auth round-trip.
        var sellers = await context.Sales
            .AsNoTracking()
            .Where(s => s.BranchId == ctx.BranchId)
            .GroupBy(s => s.SoldById)
            .Select(g => new SellerDto
            {
                Id = g.Key,
                // The seller name is a snapshot per sale; use the most recent one.
                Name = g.OrderByDescending(s => s.CreatedAt).Select(s => s.SoldByName).First()
            })
            .OrderBy(s => s.Name)
            .ToListAsync();

        return sellers;
    }
}
