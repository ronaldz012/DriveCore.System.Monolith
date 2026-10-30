using Module.Inventory.Application.UseCases.Receptions.Create;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions;

public static class StockReceptionResultProjection
{
    public static IQueryable<StockReceptionResultDto> ProjectToResult(this IQueryable<StockReception> query)
        => query.Select(r => new StockReceptionResultDto
        {
            Id = r.Id,
            Number = r.Number,
            BranchId = r.BranchId,
            ProviderId = r.ProviderId,
            ProviderName = r.Provider.Name,
            ReceivedAt = r.ReceivedAt,
            Notes = r.Notes,
            Status = r.Status,
            Items = r.Items.Select(i => new StockReceptionItemResultDto
            {
                ProductVariantId = i.ProductVariantId,
                ProductName = i.ProductVariant.Product.Name,
                VariantDescription = i.ProductVariant.Description,
                QuantityReceived = i.QuantityReceived,
                UnitCost = i.UnitCost
            }).ToList()
        });
}
