using System.Text.Json.Serialization;
using Common.Utilities;
using JevDotNet;
using JevDotNet.Models;
using Microsoft.Extensions.DependencyInjection;
using Module.Inventory.Application.UseCases.Liquidation.Probe;

namespace Module.Inventory.Infrastructure.Advisor;

/// <summary>
/// Probe: asks Jev to classify a list of well known brands by typical customer age
/// segment, using only the brand name. If the answers are accurate, the model does
/// carry real-world knowledge; if they collapse to one default, it does not — and the
/// business has to supply that context in the database.
///
/// TEMPORARY: experiment only, not part of the liquidation flow.
/// </summary>
public class BrandAudienceProber(IServiceProvider serviceProvider)
{
    private static readonly object State = new
    {
        task = "Classify the typical customer of each clothing brand by age segment.",
        options = new
        {
            youngs = "roughly 13-19, in high school, very trend driven, tight budget",
            young_adults = "roughly 20-34, working or studying at university, mid budget",
            adults = "roughly 35 and older, established income, classic and durable styles"
        },
        answer_with = "the single most typical segment for that brand, not the widest audience the brand sells to"
    };

    public async Task<Result<BrandAudienceProbeResponse>> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var jev = serviceProvider.GetService<JevClient>();
        if (jev is null)
            return new Error(ErrorCode.InvalidState, "Jev no configurado: falta la variable de entorno TypeSafeApiKey");

        JevResponse<BrandAudienceProbeResult> response;
        try
        {
            response = await jev.EvaluateAsync<BrandAudienceProbeResult>(State, cancellationToken);
        }
        catch (Exception ex)
        {
            return new Error(ErrorCode.InternalError, $"Fallo el probe de marcas con Jev: {ex.Message}");
        }

        var result = response.Result;
        var answers = new (string Brand, JevChoice<AudienceSegment>? Answer)[]
        {
            ("ZARA", result.Zara),
            ("H&M", result.Hm),
            ("SHEIN", result.Shein),
            ("NIKE", result.Nike),
            ("ADIDAS", result.Adidas),
            ("UNIQLO", result.Uniqlo),
            ("LEVI'S", result.Levis),
            ("DIESEL", result.Diesel),
            ("LACOSTE", result.Lacoste),
            ("GUESS", result.Guess),
            ("RALPH LAUREN", result.RalphLauren),
            ("TOMMY HILFIGER", result.TommyHilfiger),
        };

        var brands = answers.Select(a => a.Answer is null
            ? new BrandAudienceDto(a.Brand, string.Empty, 0, new())
            : new BrandAudienceDto(
                a.Brand,
                a.Answer.Choice.ToString(),
                a.Answer.Confidence,
                a.Answer.Probabilities.ToDictionary(p => p.Key.ToString(), p => p.Value)))
            .ToList();

        return new BrandAudienceProbeResponse(
            brands,
            response.InputTokenCount,
            response.OutputTokenCount);
    }
}
