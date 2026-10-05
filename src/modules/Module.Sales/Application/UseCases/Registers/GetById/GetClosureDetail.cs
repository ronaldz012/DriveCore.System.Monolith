using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Registers.GetById;

public class GetClosureDetail(
    ISalesDbContext context,
    IInventoryIntegrationService inventoryService)
{
    public async Task<Result<ClosureDetailDto>> Execute(ActorContext ctx, Guid id)
    {
        var closure = await GetClosureAsync(ctx.BranchId, id);
        if (closure is null)
            return GetClosureErrors.NotFound;

        return await AttachStockAsync(closure);
    }

    public async Task<Result<ClosureDetailDto>> ExecuteCurrent(ActorContext ctx)
    {
        var closureId = await GetActiveClosureIdAsync(ctx.BranchId);
        if (closureId is null)
            return GetClosureErrors.NoActiveClosure;

        return await Execute(ctx, closureId.Value);
    }

    private async Task<Guid?> GetActiveClosureIdAsync(Guid branchId)
    {
        return await context.CashRegisterClosures
            .AsNoTracking()
            .Where(c => c.BranchId == branchId && c.IsOpen)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<ClosureDetailDto?> GetClosureAsync(Guid branchId, Guid id)
    {
        return await context.CashRegisterClosures
            .AsNoTracking()
            .Where(c => c.Id == id && c.BranchId == branchId)
            .Select(c => new ClosureDetailDto
            {
                Id = c.Id,
                Number = c.Number,
                BranchId = c.BranchId,
                OpenedAt = c.OpenAt,
                ClosedAt = c.ClosedAt,
                OpenedByName = c.OpenByName,
                ClosedByName = c.CloseByName,
                OpeningBalance = c.OpeningBalance,
                SystemSalesAmount = c.SystemSalesAmount,
                RealCountedAmount = c.RealCountedAmount,
                Difference = c.RealCountedAmount - c.SystemSalesAmount,
                TotalSales = c.Sales.Sum(s => s.TotalAmount),
                CashSales = c.Sales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.TotalAmount),
                QrSales = c.Sales.Where(s => s.PaymentMethod == PaymentMethod.QrCode).Sum(s => s.TotalAmount),
                TotalExpenses = c.Movements.Where(m => m.Type == CashRegisterMovementType.Outflow).Sum(m => m.Amount),
                Sales = c.Sales
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new ClosureSaleItemDto
                    {
                        Id = s.Id,
                        Number = s.Number,
                        CreatedAt = s.CreatedAt,
                        SoldByName = s.SoldByName,
                        TotalAmount = s.TotalAmount,
                        PaymentMethod = s.PaymentMethod,
                        DocumentType = s.DocumentType,
                        Type = s.Type,
                        InvoiceNumber = s.InvoiceNumber,
                        TransactionCode = s.TransactionCode,
                        ItemsCount = s.SaleItems.Sum(si => si.Quantity),
                        HasReturn = s.Returns.Any(),
                        CanReturn = s.Type == SaleType.Sale && !s.Returns.Any(),
                        CantReturnReason = s.Type == SaleType.Return ? "IS_RETURN" : s.Returns.Any() ? "ALREADY_REFUNDED" : null,
                        Items = s.SaleItems.Select(si => new ClosureSaleItemDetailDto
                        {
                            ProductVariantId = si.ProductVariantId,
                            ProductSku = si.ProductSku,
                            ProductDisplayName = si.ProductDisplayName,
                            Quantity = si.Quantity,
                            UnitPrice = si.UnitPrice,
                            UnitCost = si.UnitCost,
                            DiscountAmount = si.DiscountAmount,
                            FinalPrice = si.FinalPrice
                        }).ToList()
                    }).ToList(),
                Movements = c.Movements
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new ClosureMovementDto
                    {
                        Id = m.Id,
                        CreatedAt = m.CreatedAt,
                        Amount = m.Amount,
                        Description = m.Description,
                        Type = m.Type
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    private async Task<Result<ClosureDetailDto>> AttachStockAsync(ClosureDetailDto closure)
    {
        var variantIds = closure.Sales
            .SelectMany(s => s.Items)
            .Select(i => i.ProductVariantId)
            .Distinct()
            .ToList();

        var stockResult = await inventoryService.GetVariantsWithStock(variantIds, closure.BranchId);
        if (!stockResult.IsSuccess)
            return stockResult.Error;

        closure.VariantStocks = stockResult.Value
            .Select(v => new ClosureVariantStockDto
            {
                ProductVariantId = v.Id,
                ProductSku = v.Sku,
                ProductDisplayName = v.DisplayName,
                CurrentStock = v.Stock
            })
            .OrderBy(v => v.CurrentStock)
            .ToList();

        return closure;
    }
}