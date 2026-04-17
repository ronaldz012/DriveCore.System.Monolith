using Inventory.Contracts.Dtos.Receptions;
using Inventory.Data.Entities.Receptions;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;

namespace Inventory.UseCases.Receptions;

public class ListReceptions(InvDbContext context)
{
    public async Task<Result<PagedResultDto<StockReceptionListDto>>> Execute(ReceptionQueryDto queryDto)
    {
        IQueryable<StockReception> query = context.StockReceptions;

        var(queryFiltered, totalCount) = query.ApplyFilters(queryDto);
        
        var receptions = queryFiltered.Where(r => r.BranchId == queryDto.BranchId)
            .Select(r => new StockReceptionListDto
            {
                Id = r.Id,
                BranchId = r.BranchId,
                ReceivedAt = r.ReceivedAt,
                Notes = r.Notes,
                Status = r.Status.ToString(),
                TotalItems = r.Items.Count,
                TotalCost = r.Items.Sum(i => i.UnitCost * i.QuantityReceived),
                Brands = r.Items.Select(x =>  x.ProductVariant.Product.Brand.Name).Distinct().ToList(),
                Categories = r.Items.Select(x =>  x.ProductVariant.Product.Category.Name).Distinct().ToList(),
            }).ToListAsync();

        return new PagedResultDto<StockReceptionListDto>
        {
            Items = await receptions,
            TotalCount = totalCount,
            Page = queryDto.GetPageValue(),
            PageSize = queryDto.GetPageSizeValue()
        };

    }
}