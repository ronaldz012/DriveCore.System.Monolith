using Common.Utilities;

namespace Module.Sales.Application.UseCases.Sales.Return;

public static class ReturnErrors
{
    public static readonly Error OriginalSaleNotFound = new(ErrorCode.NotFound, "Original sale not found");
    public static readonly Error OriginalSaleNotEligible = new(ErrorCode.InvalidState, "Original sale is not eligible for return");
    public static readonly Error AlreadyReturned = new(ErrorCode.InvalidState, "This sale already has a return");
    public static readonly Error OriginalItemNotFound = new(ErrorCode.NotFound, "Original sale item not found");
    public static readonly Error ExceedsReturnableQuantity = new(ErrorCode.ValidationError, "Return quantity exceeds sold quantity");
    public static readonly Error NoOpenCashRegister = new(ErrorCode.InvalidState, "No open cash register in this branch");
    public static readonly Error RefundAmountMismatch = new(ErrorCode.ValidationError, "The sum of payments does not match the refund total.");
    public static readonly Error ReturnCreationFailed = new(ErrorCode.InternalError, "Failed to create return");
}