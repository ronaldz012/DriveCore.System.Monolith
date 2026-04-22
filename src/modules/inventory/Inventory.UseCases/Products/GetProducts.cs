using Auth.Contracts.Interfaces;
using Inventory.Contracts.Dtos.Products;
using Inventory.Data.Entities.Products;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;
using Shared.Services;

namespace Inventory.UseCases.Products;

public class GetProducts(InvDbContext context, ICurrentUser currentUser)
{
    public async Task<Result<PagedResultDto<ListProductDto>>> Execute(ProductQueryDto queryDto)
    {
        IQueryable<Product> query = context.Products;
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
            BasePrice = p.BasePrice,
            Description = p.Description,
            InternalCode = p.InternalCode,
            Stock = p.ProductVariants
                .SelectMany(pv => pv.BranchInventories)
                .Where(bi => currentUser.BranchIds.Contains(bi.BranchId))
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