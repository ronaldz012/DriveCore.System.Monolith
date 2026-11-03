using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Domain.Documents;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Create;

public class CreateSale(
    ISalesDbContext context,
    IInventoryIntegrationService inventoryService,
    ISalesNumberGenerator numberGenerator,
    ILogger<CreateSale> logger)
{
    public async Task<Result<bool>> Execute(ActorContext ctx, CreateSaleDto dto)
    {
        var branchId = ctx.BranchId;
        var userId = ctx.UserId;
        var userName = ctx.FullName;

        var variantIds = dto.Items
            .Where(i => i.ProductVariantId.HasValue && i.ProductVariantId != Guid.Empty)
            .Select(i => i.ProductVariantId!.Value)
            .Distinct()
            .ToList();

        var cashClosure = await context.CashRegisterClosures
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.IsOpen);

        if (cashClosure is null)
            return CreateSaleErrors.NoOpenCashRegister;

        // Free items (no variant id) skip catalog validation and stock deduction.
        List<ProductVariantStockDto> variants = [];

        if (variantIds.Count > 0)
        {
            var stockResult = await inventoryService.GetVariantsWithStock(variantIds, branchId);
            if (!stockResult.IsSuccess) return stockResult.Error;

            variants = stockResult.Value;

            if (variants.Count != variantIds.Count)
                return CreateSaleErrors.ProductsNotFound;

            if (variants.Any(v => !v.IsActive))
                return CreateSaleErrors.ProductInactive;
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var factoryItems = dto.Items.Select(itemDto =>
            {
                if (itemDto.ProductVariantId is null)
                {
                    return ((Guid?)null, string.Empty, itemDto.Description!.Trim(),
                        itemDto.UnitPrice!.Value, itemDto.Quantity, itemDto.DiscountAmount, (decimal?)null);
                }

                var variant = variants.First(v => v.Id == itemDto.ProductVariantId);
                return (itemDto.ProductVariantId, variant.Sku, variant.DisplayName, variant.Price,
                    itemDto.Quantity, itemDto.DiscountAmount, (decimal?)variant.AverageCost);
            }).ToList();

            Sale sale;
            if (dto.DocumentType == DocumentType.Ticket)
            {
                sale = Sale.CreateSaleWithTicket(
                    branchId,
                    userId,
                    userName,
                    userId,
                    userName,
                    cashClosure.Id,
                    dto.PaymentMethod,
                    dto.TransactionCode,
                    dto.Notes,
                    factoryItems
                );
            }
            else
            {
                sale = Sale.CreateSaleWithInvoice(
                    branchId,
                    userId,
                    userName,
                    userId,
                    userName,
                    cashClosure.Id,
                    dto.PaymentMethod,
                    dto.DocumentType, 
                    dto.InvoiceNumber,
                    dto.TransactionCode,
                    dto.Notes,
                    factoryItems
                );
            }

            var deductions = dto.Items
                .Where(i => i.ProductVariantId.HasValue)
                .Select(i => new StockDeductionDto(i.ProductVariantId!.Value, i.Quantity))
                .ToList();

            if (deductions.Count > 0)
            {
                var deductResult = await inventoryService.DeductStock(deductions, branchId, userId, userName, sale.Id, saveChanges: false);
                if (!deductResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return deductResult.Error;
                }
            }


            sale.Number = await numberGenerator.NextAsync(context.Database, ctx.TenantId, SalesCounterKey.Sale);

            context.Sales.Add(sale);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creando venta para sucursal {BranchId}", branchId);
            return CreateSaleErrors.SaleCreationFailed;
        }
    }
}