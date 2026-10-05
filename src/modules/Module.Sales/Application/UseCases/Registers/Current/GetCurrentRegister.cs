using Common.Contracts.authentication;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;

namespace Module.Sales.Application.UseCases.Registers.Current;

public class GetCurrentRegister(ISalesDbContext context)
{
    public async Task<Result<CurrentRegisterDto>> Execute(ActorContext ctx)
    {
        var closure = await context.CashRegisterClosures
            .AsNoTracking()
            .Where(c => c.BranchId == ctx.BranchId && c.IsOpen)
            .Select(c => new CurrentRegisterDto
            {
                IsOpen = true,
                ClosureId = c.Id,
                Number = c.Number,
                OpeningBalance = c.OpeningBalance,
                OpenedAt = c.OpenAt,
                OpenByName = c.OpenByName
            })
            .FirstOrDefaultAsync();

        return closure ?? new CurrentRegisterDto { IsOpen = false };
    }
}
