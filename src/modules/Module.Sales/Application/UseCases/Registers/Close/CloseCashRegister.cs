using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Registers.Close;

public class CloseCashRegister(ISalesDbContext context)
{
    public async Task<Result<CloseCashRegisterResponseDto>> Execute(ActorContext ctx, CloseCashRegisterDto dto)
    {
        var closure = await context.CashRegisterClosures
            .Include(c => c.Sales)
                .ThenInclude(s => s.Payments)
            .Include(c => c.Movements)
            .FirstOrDefaultAsync(c => c.BranchId == ctx.BranchId && c.IsOpen);

        if (closure is null)
            return CloseCashRegisterErrors.NotFound;

        closure.Close(dto.RealCountedAmount, ctx.UserId, ctx.FullName);

        await context.SaveChangesAsync();

        // By tenders, so split sales count only their cash line.
        var cashSalesTotal = closure.Sales
            .SelectMany(s => s.Payments)
            .Where(p => p.Method == PaymentMethod.Cash)
            .Sum(p => p.Amount);

        var outflowsTotal = closure.Movements
            .Where(m => m.Type == CashRegisterMovementType.Outflow)
            .Sum(m => m.Amount);

        return new CloseCashRegisterResponseDto
        {
            Id = closure.Id,
            Number = closure.Number,
            OpeningBalance = closure.OpeningBalance,
            CashSalesTotal = cashSalesTotal,
            OutflowsTotal = outflowsTotal,
            ExpectedCash = closure.SystemSalesAmount,
            RealCountedAmount = closure.RealCountedAmount,
            Difference = closure.RealCountedAmount - closure.SystemSalesAmount,
            ClosedAt = closure.ClosedAt!.Value,
            OpenByName = closure.OpenByName,
            CloseByName = closure.CloseByName
        };
    }
}
