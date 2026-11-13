using Common.Utilities;

namespace Module.Inventory.Application.UseCases.Receptions.Edit;

public static class EditDraftErrors
{
    public static readonly Error ReceptionNotFound = new(ErrorCode.NotFound, "Reception not found");
    public static readonly Error NotDraft = new(ErrorCode.InvalidState, "Only draft receptions can be edited");
    public static readonly Error VariantsNotFound = new(ErrorCode.NotFound, "One or more variants not found");
    public static readonly Error ProductInactive = new(ErrorCode.Conflict, "One or more products are inactive and cannot be received");
    public static readonly Error ProviderNotFound = new(ErrorCode.NotFound, "Provider not found");
    public static readonly Error ProviderInactive = new(ErrorCode.Conflict, "Provider is inactive");
    public static readonly Error EditFailed = new(ErrorCode.InternalError, "Failed to update the draft reception");
}
