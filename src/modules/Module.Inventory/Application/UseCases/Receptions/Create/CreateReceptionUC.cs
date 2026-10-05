using Common.Contracts.authentication;
using Common.Domain.Documents;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Receptions;

namespace Module.Inventory.Application.UseCases.Receptions.Create;

public class CreateReceptionUc(
    IInvDbContext context,
    IInventoryNumberGenerator numberGenerator,
    ILogger<CreateReceptionUc> logger)
{
    public async Task<Result<StockReceptionResultDto>> Execute(ActorContext ctx, CreateStockReceptionDto dto)
    {
        var userId = ctx.UserId;
        var branchId = ctx.BranchId;
        var userName = ctx.FullName;

        var variantIds = dto.Items.Select(x => x.ProductVariantId).ToList();

        var variants = await context.ProductVariants
            .Include(x => x.BranchInventories)
            .Include(x => x.Product)
            .Where(x => variantIds.Contains(x.Id))
            .ToListAsync();

        var missingIds = variantIds.Except(variants.Select(x => x.Id)).ToList();
        if (missingIds.Count != 0)
            return CreateReceptionErrors.VariantsNotFound;

        if (variants.Any(v => !v.Product.IsActive))
            return CreateReceptionErrors.ProductInactive;

        var provider = await context.Providers
            .Where(p => p.Id == dto.ProviderId)
            .Select(p => new { p.IsActive })
            .FirstOrDefaultAsync();

        if (provider is null)
            return CreateReceptionErrors.ProviderNotFound;

        if (!provider.IsActive)
            return CreateReceptionErrors.ProviderInactive;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var reception = StockReception.Create(branchId, userId, userName, dto.Notes, dto.ProviderId);
            var stockMovements = new List<StockMovement>();
            var variantMap = variants.ToDictionary(v => v.Id);

            foreach (var item in dto.Items)
            {
                var variant = variantMap[item.ProductVariantId];
                var stockBefore = variant.GetStockByBranch(branchId);
                var stockAfter = stockBefore + item.QuantityReceived;

                reception.AddExistingVariant(variant.Id, userId, userName, item.QuantityReceived, item.UnitCost);
                variant.RegisterPurchase(item.QuantityReceived, item.UnitCost);
                variant.AddQuantity(item.QuantityReceived, branchId, userId, userName);

                stockMovements.Add(StockMovement.CreateReception(
                    branchId, variant.Id, userId, userName, item.QuantityReceived, reception.Id, item.UnitCost, stockBefore, stockAfter));
            }

            reception.Number = await numberGenerator.NextAsync(context.Database, ctx.TenantId, InventoryCounterKey.Reception);

            context.StockReceptions.Add(reception);
            context.StockMovements.AddRange(stockMovements);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            var result = await context.StockReceptions
                .Where(r => r.Id == reception.Id)
                .Select(r => new StockReceptionResultDto
                {
                    Id = r.Id,
                    Number = r.Number,
                    BranchId = r.BranchId,
                    ProviderId = r.ProviderId,
                    ProviderName = r.Provider.Name,
                    ReceivedAt = r.ReceivedAt,
                    Notes = r.Notes,
                    Items = r.Items.Select(i => new StockReceptionItemResultDto
                    {
                        ProductVariantId = i.ProductVariantId,
                        ProductName = i.ProductVariant.Product.Name,
                        VariantDescription = i.ProductVariant.Description,
                        QuantityReceived = i.QuantityReceived,
                        UnitCost = i.UnitCost
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (result == null)
                return CreateReceptionErrors.ReceptionQueryFailed;

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error al crear recepción para sucursal {BranchId}", branchId);
            return CreateReceptionErrors.CreationFailed;
        }
    }
}
