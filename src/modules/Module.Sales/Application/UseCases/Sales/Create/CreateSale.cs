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

        var variantIds = dto.Items.Select(i => i.ProductVariantId).Distinct().ToList();

        var stockResult = await inventoryService.GetVariantsWithStock(variantIds, branchId);
        if (!stockResult.IsSuccess) return stockResult.Error;

        var cashClosure = await context.CashRegisterClosures
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.IsOpen);

        if (cashClosure is null)
            return CreateSaleErrors.NoOpenCashRegister;

        var variants = stockResult.Value;
        if (variants.Count != variantIds.Count)
            return CreateSaleErrors.ProductsNotFound;

        if (variants.Any(v => !v.IsActive))
            return CreateSaleErrors.ProductInactive;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var factoryItems = dto.Items.Select(itemDto =>
            {
                var variant = variants.First(v => v.Id == itemDto.ProductVariantId);
                return (itemDto.ProductVariantId, variant.Sku, variant.DisplayName, variant.Price, itemDto.Quantity, itemDto.DiscountAmount, variant.AverageCost);
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
                .Select(i => new StockDeductionDto(i.ProductVariantId, i.Quantity))
                .ToList();

            var deductResult = await inventoryService.DeductStock(deductions, branchId, userId, userName, sale.Id, saveChanges: false);
            if (!deductResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return deductResult.Error;
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