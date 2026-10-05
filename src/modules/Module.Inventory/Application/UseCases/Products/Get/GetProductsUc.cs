using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Products;

namespace Module.Inventory.Application.UseCases.Products.Get;

public class GetProductsUc(IInvDbContext context)
{
    public async Task<Result<PagedResultDto<ListProductRequest>>> Execute(ActorContext ctx, ProductQueryDto queryDto)
    {
        var query = context.Products.AsQueryable();
        if (!string.IsNullOrWhiteSpace(queryDto.Filter))
        {
            var words = queryDto.Filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                var pattern = $"%{word}%";
                query = query.Where(x =>
                    EF.Functions.ILike(x.Name, pattern) ||
                    (x.InternalCode != null && EF.Functions.ILike(x.InternalCode, pattern)) ||
                    EF.Functions.ILike(x.Brand.Name, pattern));
            }
        }

        if (queryDto.IncludeInactive != true)
        {
            query = query.Where(x => x.IsActive);
        }

        if (queryDto.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == queryDto.CategoryId);
        }

        if (queryDto.Gender.HasValue)
        {
            query = query.Where(x => x.Gender == queryDto.Gender);
        }
        
        var totalCount = await query.CountAsync();

        var descending = queryDto.SortDescending ?? true;

        IOrderedQueryable<Product> orderedQuery;
        if (queryDto.SortBy == ProductSortBy.Stock)
        {
            orderedQuery = descending
                ? query
                    .OrderByDescending(p => p.ProductVariants
                        .SelectMany(pv => pv.BranchInventories)
                        .Where(bi => ctx.BranchIds.Contains(bi.BranchId))
                        .Sum(bi => bi.Stock))
                    .ThenBy(p => p.Name)
                    .ThenBy(p => p.Id)
                : query
                    .OrderBy(p => p.ProductVariants
                        .SelectMany(pv => pv.BranchInventories)
                        .Where(bi => ctx.BranchIds.Contains(bi.BranchId))
                        .Sum(bi => bi.Stock))
                    .ThenBy(p => p.Name)
                    .ThenBy(p => p.Id);
        }
        else
        {
            orderedQuery = descending
                ? query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id)
                : query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id);
        }

        var pagedIds = await orderedQuery
            .Skip((queryDto.Page - 1) * queryDto.PageSize)
            .Take(queryDto.PageSize)
            .Select(p => p.Id)
            .ToListAsync();

        if (pagedIds.Count == 0)
            return new PagedResultDto<ListProductRequest>
            {
                TotalCount = totalCount,
                Items = [],
                Page = queryDto.Page,
                PageSize = queryDto.PageSize
            };

        var items = await context.Products
            .Where(p => pagedIds.Contains(p.Id))
            .Select(p => new ListProductRequest
            {
                Id = p.Id,
                Name = p.Name,
                BrandName = p.Brand.Name,
                CategoryName = p.Category.Name,
                BasePrice = p.BasePrice,
                InternalCode = p.InternalCode,
                IsActive = p.IsActive,
                VariantsCount = p.ProductVariants.Count,
                TotalStock = p.ProductVariants
                    .SelectMany(pv => pv.BranchInventories)
                    .Where(bi => ctx.BranchIds.Contains(bi.BranchId))
                    .Sum(bi => bi.Stock),
            })
            .ToListAsync();

        var byId = items.ToDictionary(i => i.Id);
        var ordered = pagedIds.Select(id => byId[id]).ToList();

        return new PagedResultDto<ListProductRequest>
        {
            TotalCount = totalCount,
            Items = ordered,
            Page = queryDto.Page,
            PageSize = queryDto.PageSize
        };
    }

}