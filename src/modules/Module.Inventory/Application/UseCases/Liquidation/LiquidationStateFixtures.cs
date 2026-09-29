namespace Module.Inventory.Application.UseCases.Liquidation;

/// <summary>
/// Entry point for building the state. Picks the candidate set by axis.
/// Replaced by BuildLiquidationState reading the database.
/// </summary>
public static class LiquidationStateFixtures
{
    public static LiquidationStateDto Sample(
        LiquidationDataset dataset,
        string branchName,
        string tenantLabel,
        int windowDays,
        DateTime? eventDate,
        ObjectiveType? objectiveType,
        MarginFloor? marginFloor,
        string? userNotes = null) =>
        dataset switch
        {
            LiquidationDataset.Seasonal => SeasonalStateFixture.Sample(
                branchName, tenantLabel, windowDays, eventDate, objectiveType, marginFloor, userNotes),

            LiquidationDataset.Workwear => WorkwearStateFixture.Sample(
                branchName, tenantLabel, windowDays, eventDate, objectiveType, marginFloor, userNotes),

            _ => LiquidationStateFixture.Sample(
                branchName, tenantLabel, windowDays, eventDate, objectiveType, marginFloor, userNotes)
        };
}
