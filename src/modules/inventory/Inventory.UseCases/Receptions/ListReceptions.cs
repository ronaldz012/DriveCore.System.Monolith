using Auth.Contracts.Interfaces;
using Inventory.Contracts.Dtos.Receptions;
using Inventory.Data.Entities.Receptions;
using Inventory.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Result;
using Shared.Services;

namespace Inventory.UseCases.Receptions;

public class ListReceptions(InvDbContext context, ICurrentUser currentUser)
{
    public async Task<Result<PagedResultDto<StockReceptionListDto>>> Execute(ReceptionQueryDto queryDto)
    {
        IQueryable<StockReception> query = context.StockReceptions;
        var branches = currentUser.BranchIds;

        var (queryFiltered, totalCount) = query.ApplyFilters(queryDto);

        var tempReceptions = await queryFiltered
            .Where(r => branches.Contains(r.BranchId))
            .Select(r => new
            {
                r.Id,
                r.BranchId,
                r.ReceivedAt,
                r.Notes,
                Status = r.Status.ToString(),
                TotalItems = r.Items.Count,
                TotalCost = r.Items.Sum(i => i.UnitCost * i.QuantityReceived),
                RawItems = r.Items.Select(i => new
                {
                    CategoryName = i.ProductVariant.Product.Category.Name,
                    BrandName = i.ProductVariant.Product.Brand.Name
                }).ToList()
            }).AsNoTracking().ToListAsync();

        var receptions = tempReceptions.Select(r => new StockReceptionListDto
        {
            Id = r.Id,
            BranchId = r.BranchId,
            ReceivedAt = r.ReceivedAt,
            Notes = r.Notes,
            Status = r.Status,
            TotalItems = r.TotalItems,
            TotalCost = r.TotalCost,
            // Ahora sí aplicamos GroupBy y ToDictionary sobre la lista en memoria
            Types = r.RawItems
                .GroupBy(x => x.CategoryName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.BrandName).Distinct().ToList()
                )
        }).ToList();


        return new PagedResultDto<StockReceptionListDto>
        {
            Items = receptions,
            TotalCount = totalCount,
            Page = queryDto.GetPageValue(),
            PageSize = queryDto.GetPageSizeValue()
        };

    }
}