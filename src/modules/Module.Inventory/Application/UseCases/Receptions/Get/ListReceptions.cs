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
        var currentBranchId = ctx.BranchIds[0];

        query = query.Where(x => x.BranchId == currentBranchId);

        if (queryDto.DateFrom.HasValue)
            query = query.Where(x => x.ReceivedAt >= queryDto.DateFrom.Value);

        if (queryDto.DateTo.HasValue)
            query = query.Where(x => x.ReceivedAt <= queryDto.DateTo.Value);


        if (queryDto.Status != null)
            query = query.Where(x => x.Status == queryDto.Status);

        if (queryDto.BrandId != null)
            query = query.Where(x =>
                x.Items.Any(i => i.ProductVariant.Product.BrandId == queryDto.BrandId));

        var filter = queryDto.Filter?.Trim();

        if (!string.IsNullOrEmpty(filter))
        {
            var isNumber = filter.Length <= 9 && filter.All(char.IsAsciiDigit);

            if (isNumber && int.TryParse(filter, out var number))
                query = query.Where(x => x.Number == number);
            else
            {
                var words = filter.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    var pattern = $"%{word}%";

                    query = query.Where(x =>
                        (x.Notes != null && EF.Functions.ILike(x.Notes, pattern)) ||
                        EF.Functions.ILike(x.Provider.Name, pattern) ||
                        x.Items.Any(i => EF.Functions.ILike(i.ProductVariant.Product.Brand.Name, pattern)));
                }
            }
        }

        var totalCount = await query.CountAsync();


        var receptions = await query
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
                Notes = r.Notes,
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