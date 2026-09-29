namespace Module.Inventory.Application.UseCases.Liquidation;

/// <summary>
/// Builds the context block of the state, shared by every candidate set so the
/// three datasets only differ in their products.
/// </summary>
public static class LiquidationContextFactory
{
    public static LiquidationContextDto Create(
        string branchName,
        string tenantLabel,
        int windowDays,
        DateTime? eventDate,
        ObjectiveType? objectiveType,
        MarginFloor? marginFloor,
        string? userNotes)
    {
        var today = DateTime.UtcNow.Date;
        int? daysUntilEvent = eventDate.HasValue
            ? (int)(eventDate.Value.Date - today).TotalDays
            : null;

        return new LiquidationContextDto(
            branchName, today, windowDays, tenantLabel, eventDate, daysUntilEvent,
            objectiveType.HasValue ? new ObjectiveDto(objectiveType.Value) : null,
            marginFloor, userNotes);
    }
}
