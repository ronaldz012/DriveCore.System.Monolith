using System.ComponentModel.DataAnnotations;

namespace Module.Inventory.Application.UseCases.Liquidation;

public class AdviseLiquidationRequest
{
    public LiquidationDataset Dataset { get; set; } = LiquidationDataset.Default;

    [Range(1, 365, ErrorMessage = "Window must be between 1 and 365 days")]
    public int WindowDays { get; set; } = 90;

    public DateTime? EventDate { get; set; }
    public ObjectiveType? ObjectiveType { get; set; }
    public MarginFloor? MarginFloor { get; set; }

    /// <summary>
    /// Free-form context from the user (no practical limit: the more specific, the better Jev decides).
    /// Ex: "the semester starts in 10 days, prioritize basics and light outerwear, leave supplier Paz alone".
    /// </summary>
    public string? UserNotes { get; set; }
}
