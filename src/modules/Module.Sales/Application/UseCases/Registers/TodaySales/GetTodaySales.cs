using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Registers.TodaySales;

public class GetTodaySales(ISalesDbContext context)
{
    public async Task<Result<TodaySalesDto>> Execute(ActorContext ctx)
    {
        var closure = await context.CashRegisterClosures
            .AsNoTracking()
            .Where(c => c.BranchId == ctx.BranchId && c.IsOpen)
            .Select(c => new
            {
                c.Id,
                c.Number,
                c.OpeningBalance,
                c.OpenAt,
                c.OpenByName,
                Sales = c.Sales.Select(s => new
                {
                    s.TotalAmount,
                    s.PaymentMethod,
                    s.DocumentType,
                    ItemsCount = s.SaleItems.Sum(si => si.Quantity)
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (closure is null)
            return new TodaySalesDto { IsOpen = false };

        var dto = new TodaySalesDto
        {
            IsOpen = true,
            ClosureId = closure.Id,
            Number = closure.Number,
            OpeningBalance = closure.OpeningBalance,
            OpeningAt = closure.OpenAt,
            OpenedByName = closure.OpenByName
        };

        foreach (var sale in closure.Sales)
        {
            dto.SalesCount++;
            dto.TotalAmount += sale.TotalAmount;
            dto.TotalItems += sale.ItemsCount;
            dto.CashAmount += sale.PaymentMethod == PaymentMethod.Cash ? sale.TotalAmount : 0;
            dto.QrCodeAmount += sale.PaymentMethod == PaymentMethod.QrCode ? sale.TotalAmount : 0;
            dto.TicketCount += sale.DocumentType == DocumentType.Ticket ? 1 : 0;
            dto.InvoiceCount += sale.DocumentType == DocumentType.Invoice ? 1 : 0;
        }

        dto.AverageTicket = dto.SalesCount > 0 ? dto.TotalAmount / dto.SalesCount : 0;

        return dto;
    }
}
