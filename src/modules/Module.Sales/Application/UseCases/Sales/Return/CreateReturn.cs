using Common.Contracts.authentication;
using Common.Contracts.inventory;
using Common.Domain.Documents;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Sales.Return;

public class CreateReturn(
    ISalesDbContext context,
    IInventoryIntegrationService inventoryService,
    ISalesNumberGenerator numberGenerator,
    ILogger<CreateReturn> logger)
{
    public async Task<Result<CreateReturnResponse>> Execute(ActorContext ctx, CreateReturnDto dto)
    {
        var originalSale = await context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.Id == dto.OriginalSaleId && s.TenantId == ctx.TenantId);

        if (originalSale == null)
            return ReturnErrors.OriginalSaleNotFound;

        if (originalSale.Type != SaleType.Sale)
            return ReturnErrors.OriginalSaleNotEligible;

        var alreadyReturned = await context.Sales
            .AnyAsync(s => s.OriginalSaleId == dto.OriginalSaleId && s.Type == SaleType.Return);
        if (alreadyReturned)
            return ReturnErrors.AlreadyReturned;

        var originalItemIds = originalSale.SaleItems.Select(i => i.Id).ToHashSet();
        var invalidItems = dto.Items.Where(i => !originalItemIds.Contains(i.OriginalSaleItemId)).ToList();
        if (invalidItems.Any())
            return ReturnErrors.OriginalItemNotFound;

        foreach (var item in dto.Items)
        {
            var originalItem = originalSale.SaleItems.First(i => i.Id == item.OriginalSaleItemId);
            if (item.Quantity > originalItem.Quantity)
                return ReturnErrors.ExceedsReturnableQuantity;
        }

        var openClosure = await context.CashRegisterClosures
            .FirstOrDefaultAsync(c => c.BranchId == ctx.BranchId && c.IsOpen);
        if (openClosure == null)
            return ReturnErrors.NoOpenCashRegister;

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var returnSale = Sale.CreateReturn(
                branchId: ctx.BranchId,
                soldById: ctx.UserId,
                soldByName: ctx.FullName,
                createdBy: ctx.UserId,
                createdByName: ctx.FullName,
                cashRegisterClosureId: openClosure.Id,
                paymentMethod: PaymentMethod.Cash,
                documentType: DocumentType.Ticket,
                transactionCode: null,
                notes: $"Devolución de venta {dto.OriginalSaleId}",
                originalSaleId: dto.OriginalSaleId,
                items: dto.Items.Select(item =>
                {
                    var originalItem = originalSale.SaleItems.First(i => i.Id == item.OriginalSaleItemId);
                    return (originalItem.ProductVariantId, originalItem.ProductSku, originalItem.ProductDisplayName,
                            originalItem.UnitPrice, item.Quantity, originalItem.DiscountAmount, originalItem.UnitCost, item.OriginalSaleItemId);
                }).ToList()
            );

            returnSale.Number = await numberGenerator.NextAsync(context.Database, ctx.TenantId, SalesCounterKey.Sale);

            context.Sales.Add(returnSale);

            var returns = dto.Items.Select(item =>
            {
                var originalItem = originalSale.SaleItems.First(i => i.Id == item.OriginalSaleItemId);
                return new StockReturnDto(originalItem.ProductVariantId, item.Quantity, originalItem.UnitCost);
            }).ToList();

            var returnResult = await inventoryService.ReturnStock(
                returns, ctx.BranchId, ctx.UserId, ctx.FullName, returnSale.Id, saveChanges: false);
            if (!returnResult.IsSuccess)
            {
                await transaction.RollbackAsync();
                return returnResult.Error;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CreateReturnResponse(
                returnSale.Id,
                $"RET-{returnSale.Id.ToString()[..8].ToUpper()}",
                Math.Abs(returnSale.TotalAmount));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creating return for sale {OriginalSaleId}", dto.OriginalSaleId);
            return ReturnErrors.ReturnCreationFailed;
        }
    }
}