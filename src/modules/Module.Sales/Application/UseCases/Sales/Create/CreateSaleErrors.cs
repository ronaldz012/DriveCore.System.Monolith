using Common.Utilities;

namespace Module.Sales.Application.UseCases.Sales.Create;

public static class CreateSaleErrors
{
    public static readonly Error ProductsNotFound = new(ErrorCode.NotFound, "One or more products do not exist.");
    public static readonly Error ProductInactive = new(ErrorCode.Conflict, "One or more products are inactive and cannot be sold.");
    public static readonly Error NoOpenCashRegister = new(ErrorCode.InvalidState, "No open cash register for this branch.");
    public static readonly Error PaymentAmountMismatch = new(ErrorCode.ValidationError, "The sum of payments does not match the sale total.");
    public static readonly Error SaleCreationFailed = new(ErrorCode.InternalError, "Failed to create sale.");
}
