using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Domain.Inventory;
using Module.Inventory.Domain.Products;
using Module.Inventory.Domain.Transfers;

namespace Module.Inventory.Infrastructure;

public class InventoryIntegrationService(IInvDbContext context) : IInventoryIntegrationService
{
    public async Task<Result<List<ProductVariantStockDto>>> GetVariantsWithStock(List<Guid> variantIds, Guid branchId)
    {
        var result = await context.ProductVariants
            .Where(pv => variantIds.Contains(pv.Id))
            .Select(pv => new
            {
                pv.Id,
                pv.Sku,
                pv.Price,
                pv.AverageCost,
                IsActive = pv.Product.IsActive,
                SizeName = pv.Size.Name,
                ColorName = pv.Color.Name,
                ProductName = pv.Product.Name,
                CategoryName = pv.Product.Category.Name,
                BrandName = pv.Product.Brand.Name,
                Stock = pv.BranchInventories
                    .Where(bi => bi.BranchId == branchId)
                    .Select(bi => bi.Stock)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var dtos = result
            .Select(x => new ProductVariantStockDto(
                x.Id,
                x.Sku,
                ProductVariant.BuildDisplayName(x.CategoryName, x.BrandName, x.ProductName, x.ColorName, x.SizeName),
                x.Price,
                x.Stock,
                x.IsActive,
                x.AverageCost))
            .ToList();

        return dtos;
    }

    public async Task<Result<bool>> DeductStock(
    List<StockDeductionDto> deductions, Guid branchId, Guid userId, string userName, Guid referenceId, bool saveChanges = true)
{
    var variantIds = deductions.Select(d => d.ProductVariantId).ToList();

    var variants = await context.ProductVariants
        .Include(pv => pv.BranchInventories.Where(bi => bi.BranchId == branchId))
        .Where(pv => variantIds.Contains(pv.Id))
        .ToListAsync();

    foreach (var deduction in deductions)
    {
        var pv = variants.FirstOrDefault(v => v.Id == deduction.ProductVariantId);
        if (pv == null)
        {
            return new Error(ErrorCode.NotFound, $"Product variant {deduction.ProductVariantId} not found.");
        }
        if (!pv.HasSufficientStock(deduction.Quantity, branchId))
        {
            return new Error(ErrorCode.InvalidState, $"Insufficient stock for product {pv.Sku}.");
        }
        pv.SellStock(deduction.Quantity, branchId, userId, userName, referenceId);
    }

    if (saveChanges)
        await context.SaveChangesAsync();

    return true; 
}

    public async Task<bool> BranchHasPendingTransfers(Guid branchId)
    {
        return await context.StockTransfers
            .AnyAsync(t =>
                (t.FromBranchId == branchId || t.ToBranchId == branchId) &&
                (t.Status == TransferStatus.Pending || t.Status == TransferStatus.Transit));
    }

    public async Task<Result<bool>> ReturnStock(
        List<StockReturnDto> returns, Guid branchId, Guid userId, string userName, Guid referenceId, bool saveChanges = true)
    {
        var variantIds = returns.Select(r => r.ProductVariantId).ToList();

        var variants = await context.ProductVariants
            .Include(pv => pv.BranchInventories)
            .Where(pv => variantIds.Contains(pv.Id))
            .ToListAsync();

        foreach (var returnItem in returns)
        {
            var pv = variants.FirstOrDefault(v => v.Id == returnItem.ProductVariantId);
            if (pv == null)
                return new Error(ErrorCode.NotFound, $"Product variant {returnItem.ProductVariantId} not found.");

            // Re-adquisición al costo histórico: recalcula AVCO sobre stock GLOBAL antes de sumar stock en la branch
            var stockBefore = pv.GetStockByBranch(branchId);
            pv.RegisterPurchase(returnItem.Quantity, returnItem.UnitCost);
            pv.AddQuantity(returnItem.Quantity, branchId, userId, userName);
            var stockAfter = pv.GetStockByBranch(branchId);

            // Crear movimiento de devolución con el costo de la venta original
            context.StockMovements.Add(StockMovement.CreateReturn(
                branchId, returnItem.ProductVariantId, userId, userName, 
                returnItem.Quantity, referenceId, returnItem.UnitCost, stockBefore, stockAfter));
        }

        if (saveChanges)
            await context.SaveChangesAsync();

        return true;
    }
}