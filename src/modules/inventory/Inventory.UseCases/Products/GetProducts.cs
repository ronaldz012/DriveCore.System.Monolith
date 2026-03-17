using Inventory.Contracts.Dtos.Products;
using Inventory.Data.Entities.Products;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;

namespace Inventory.UseCases.Products;

public class GetProducts(InvDbContext context)
{
    public async Task<Result<PagedResultDto<ListProductDto>>> Execute(ProductQueryDto queryDto)
    {
        IQueryable<Product>  query = context.Products;
        if (!string.IsNullOrEmpty(queryDto.Filter))
        {
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{queryDto.Filter}%"));
        }
    
        if (queryDto.BrandId.HasValue)
        {
            query = query.Where(x => x.BrandId == queryDto.BrandId);
        }

        if (queryDto.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == queryDto.CategoryId);
        }
        var (filteredQuery, totalCount) = query.ApplyFilters(queryDto);
        var items = await filteredQuery.Select(p => new ListProductDto()
        {
            Id = p.Id,
            Name = p.Name,
            BasePrice = 100m,
            Description = p.Description,
            Stock = p.ProductVariants
                .SelectMany(pv => pv.BranchInventories)
                .Where(bi => bi.BranchId == queryDto.BranchId)
                .Sum(bi => bi.Stock),
        }).ToListAsync();
        return new PagedResultDto<ListProductDto>()
        {
            TotalCount = totalCount,
            Items = items,
            Page = queryDto.GetPageValue(),
            PageSize = queryDto.GetPageSizeValue()
        };
    }
        
}