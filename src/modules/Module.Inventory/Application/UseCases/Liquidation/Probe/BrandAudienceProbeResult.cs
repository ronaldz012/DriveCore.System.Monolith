using JevDotNet.Models;

namespace Module.Inventory.Application.UseCases.Liquidation.Probe;

/// <summary>
/// One question per brand. In the declarative API the question id is the property name,
/// so the brand has to be named inside the constant question text.
/// Using JevChoice&lt;T&gt; instead of the bare enum returns the full probability
/// distribution, which is what makes the probe informative.
/// </summary>
public class BrandAudienceProbeResult
{
    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'ZARA'?")]
    public JevChoice<AudienceSegment>? Zara { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'H&M'?")]
    public JevChoice<AudienceSegment>? Hm { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'SHEIN'?")]
    public JevChoice<AudienceSegment>? Shein { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'NIKE'?")]
    public JevChoice<AudienceSegment>? Nike { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'ADIDAS'?")]
    public JevChoice<AudienceSegment>? Adidas { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'UNIQLO'?")]
    public JevChoice<AudienceSegment>? Uniqlo { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'LEVI'S'?")]
    public JevChoice<AudienceSegment>? Levis { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'DIESEL'?")]
    public JevChoice<AudienceSegment>? Diesel { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'LACOSTE'?")]
    public JevChoice<AudienceSegment>? Lacoste { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'GUESS'?")]
    public JevChoice<AudienceSegment>? Guess { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'RALPH LAUREN'?")]
    public JevChoice<AudienceSegment>? RalphLauren { get; set; }

    [JevChoiceQuestion<AudienceSegment>("Which age segment is the typical customer of the brand 'TOMMY HILFIGER'?")]
    public JevChoice<AudienceSegment>? TommyHilfiger { get; set; }
}

public record BrandAudienceDto(
    string Brand,
    string Choice,
    double Confidence,
    Dictionary<string, double> Probabilities);

public record BrandAudienceProbeResponse(
    List<BrandAudienceDto> Brands,
    long InputTokens,
    long OutputTokens);
