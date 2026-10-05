using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Get;

public class ListReceptions(IInvDbContext context)
{
    public async Task<Result<PagedResultDto<StockReceptionListDto>>> Execute(ActorContext ctx, ReceptionQueryDto queryDto)
    {
        IQueryable<StockReception> query = context.StockReceptions;
        var branches = ctx.BranchIds;

        if (queryDto.DateFrom.HasValue)
            query = query.Where(x => x.ReceivedAt >= queryDto.DateFrom.Value);

        if (queryDto.DateTo.HasValue)
            query = query.Where(x => x.ReceivedAt <= queryDto.DateTo.Value);


        if (queryDto.Status != null)
            query = query.Where(x => x.Status == queryDto.Status);

        if (queryDto.BrandId != null)
            query = query.Where(x =>
                x.Items.Any(i => i.ProductVariant.Product.BrandId == queryDto.BrandId));

        var totalCount = await query.CountAsync();


        var receptions = await query
            .Where(x => x.BranchId == ctx.BranchIds[0])
            .OrderByDescending(r => r.ReceivedAt)
            .ApplyPagination(queryDto)
            .Select(r => new StockReceptionListDto
            {
                Id = r.Id,
                Number = r.Number,
                BranchId = r.BranchId,
                ProviderId = r.ProviderId,
                ProviderName = r.Provider.Name,
                ReceivedAt = r.ReceivedAt,
                Status = r.Status,
                TotalItems = r.Items.Sum(x => x.QuantityReceived),
                ProductVariantsCount = r.Items.Count,
                TotalCost = r.Items.Sum(i => i.UnitCost * i.QuantityReceived),
                BrandNames = r.Items.Select(x => x.ProductVariant.Product.Brand.Name).Distinct().ToList(),
                CategoryNames = r.Items.Select(x => x.ProductVariant.Product.Category.Name).Distinct().ToList()
            }).ToListAsync();
            

        return new PagedResultDto<StockReceptionListDto>
        {
            Items = receptions,
            TotalCount = totalCount,
            Page = queryDto.Page,
            PageSize = queryDto.PageSize
        };

    }
}