using System.ComponentModel;

namespace Module.Inventory.Application.UseCases.Liquidation.Probe;

/// <summary>
/// Age segments used to probe whether the model carries real-world knowledge about
/// who buys a given brand. The [Description] text is what the model reads as the
/// definition of each option, so it has to be self-explanatory.
/// </summary>
public enum AudienceSegment
{
    [Description("Youngs: roughly 13 to 19 years old, still in high school, very trend driven, tight budget, follows whatever is on social media")]
    Youngs,

    [Description("Young adults: roughly 20 to 34 years old, working or studying at university, mid budget, wants versatile everyday clothes")]
    YoungAdults,

    [Description("Adults: roughly 35 and older, established income, prefers classic and durable styles over trends, less price sensitive")]
    Adults
}
