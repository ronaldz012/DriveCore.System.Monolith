using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Search;

public class SearchSalesBySku(ISalesDbContext context)
{
    public async Task<Result<SkuSearchResponseDto>> Execute(ActorContext ctx, SkuSearchQueryDto queryDto)
    {
        var currentBranch = ctx.BranchIds[0];
        var dateFrom = DateTime.UtcNow.AddDays(-queryDto.Days);

        var query = context.Sales
            .AsNoTracking()
            .Where(s => s.BranchId == currentBranch
                     && s.Type == SaleType.Sale
                     && s.CreatedAt >= dateFrom
                     && !s.Returns.Any()
                     && s.SaleItems.Any(si => si.ProductSku == queryDto.Sku));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ApplyPagination(queryDto)
            .Select(s => new SaleSkuSearchDto
            {
                Id = s.Id,
                Number = s.Number,
                CreatedAt = s.CreatedAt,
                TotalAmount = s.TotalAmount,
                SoldByName = s.SoldByName,
                TotalItems = s.SaleItems.Count,
                TotalUnitsSold = s.SaleItems.Sum(si => si.Quantity),
                MatchedItem = s.SaleItems
                    .Where(si => si.ProductSku == queryDto.Sku)
                    .Select(si => new MatchedItemDto
                    {
                        SaleItemId = si.Id,
                        Quantity = si.Quantity,
                        UnitPrice = si.UnitPrice
                    }).FirstOrDefault()!
            })
            .ToListAsync();

        var displayName = await context.SaleItems
            .Where(si => si.ProductSku == queryDto.Sku)
            .Select(si => si.ProductDisplayName)
            .FirstOrDefaultAsync() ?? string.Empty;

        return new SkuSearchResponseDto
        {
            SearchedSku = queryDto.Sku,
            SearchedDisplayName = displayName,
            Sales = new PagedResultDto<SaleSkuSearchDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageSize = queryDto.PageSize,
                Page = queryDto.Page
            }
        };
    }
}
