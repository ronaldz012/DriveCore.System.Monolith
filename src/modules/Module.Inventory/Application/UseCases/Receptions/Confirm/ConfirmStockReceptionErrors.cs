using Common.Utilities;

namespace Module.Inventory.Application.UseCases.Receptions.Confirm;

public static class ConfirmStockReceptionErrors
{
    public static readonly Error ReceptionNotFound = new(ErrorCode.NotFound, "Reception not found");
    public static readonly Error NotDraft = new(ErrorCode.InvalidState, "Only draft receptions can be confirmed");
    public static readonly Error MissingUnitCosts = new(ErrorCode.Conflict, "All items must have a unit cost before confirming");
    public static readonly Error VariantsNotFound = new(ErrorCode.NotFound, "One or more variants not found");
    public static readonly Error ProductInactive = new(ErrorCode.Conflict, "One or more products are inactive and cannot be received");
    public static readonly Error ReceptionQueryFailed = new(ErrorCode.InternalError, "Reception was confirmed but could not be retrieved");
    public static readonly Error ConfirmFailed = new(ErrorCode.InternalError, "Failed to confirm reception");
}
