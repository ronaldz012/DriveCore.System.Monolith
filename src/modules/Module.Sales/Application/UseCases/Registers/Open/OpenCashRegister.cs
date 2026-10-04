using Common.Contracts.authentication;
using Common.Domain.Documents;
using Common.Utilities;
using Microsoft.EntityFrameworkCore;
using Module.Sales.Application.Abstraction;
using Module.Sales.Domain;

namespace Module.Sales.Application.UseCases.Registers.Open;

public static class OpenCashRegisterErrors
{
    public static readonly Error BranchMismatch = new(ErrorCode.Conflict, "Branch does not belong to the current user.");
    public static readonly Error AlreadyOpen = new(ErrorCode.Conflict, "There is already an open cash register for this branch.");
    public static readonly Error Failed = new(ErrorCode.InternalError, "Could not open cash register.");
}

public class OpenCashRegister(ISalesDbContext context, ISalesNumberGenerator numberGenerator)
{
    public async Task<Result<Guid>> Execute(ActorContext ctx, OpenCashRegisterDto dto)
    {
        var branchId = ctx.BranchId;

        var alreadyOpen = await context.CashRegisterClosures
            .AnyAsync(c => c.BranchId == branchId && c.IsOpen);

        if (alreadyOpen)
            return OpenCashRegisterErrors.AlreadyOpen;

        var closure = CashRegisterClosure.Open(branchId, dto.OpeningBalance, ctx.UserId, ctx.FullName);


        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
 
            closure.Number = await numberGenerator.NextAsync(context.Database, ctx.TenantId, SalesCounterKey.CashClose);

            context.CashRegisterClosures.Add(closure);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return closure.Id;
    }
}
