using Common.Utilities;

namespace Module.Inventory.Application.UseCases.Receptions.GetLabels;

public static class ReceptionLabelsErrors
{
    public static readonly Error ReceptionNotFound = new(ErrorCode.NotFound, "Reception not found");
    public static readonly Error ReceptionNotConfirmed = new(ErrorCode.InvalidState, "Labels are only available for confirmed receptions");
}
