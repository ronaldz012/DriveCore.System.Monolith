using Common.Utilities;

namespace Module.Inventory.Application.UseCases.Receptions.Create;

public static class CreateReceptionErrors
{
    public static readonly Error ItemsRequired = new(ErrorCode.ValidationError, "At least one item is required to confirm a reception");
    public static readonly Error VariantsNotFound = new(ErrorCode.NotFound, "One or more variants not found");
    public static readonly Error ProviderNotFound = new(ErrorCode.NotFound, "Provider not found");
    public static readonly Error ProviderInactive = new(ErrorCode.Conflict, "Provider is deactivated");
    public static readonly Error ProductInactive = new(ErrorCode.Conflict, "One or more products are inactive and cannot be received");
    public static readonly Error ReceptionQueryFailed = new(ErrorCode.InternalError, "Reception was saved but could not be retrieved");
    public static readonly Error CreationFailed = new(ErrorCode.InternalError, "Failed to create reception");
}
