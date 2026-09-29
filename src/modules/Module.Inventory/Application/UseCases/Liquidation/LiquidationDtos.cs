using System.Text.Json.Serialization;

namespace Module.Inventory.Application.UseCases.Liquidation;

public record LiquidationStateDto(
    [property: JsonPropertyName("context")] LiquidationContextDto Context,
    [property: JsonPropertyName("products")] List<ProductStateDto> Products);

public record LiquidationContextDto(
    [property: JsonPropertyName("branch")] string Branch,
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("window_days")] int WindowDays,
    [property: JsonPropertyName("tenant")] string Tenant,
    [property: JsonPropertyName("event_date")] DateTime? EventDate,
    [property: JsonPropertyName("days_until_event")] int? DaysUntilEvent,
    [property: JsonPropertyName("objective")] ObjectiveDto? Objective,
    [property: JsonPropertyName("margin_floor")] MarginFloor? MarginFloor,
    [property: JsonPropertyName("user_notes")] string? UserNotes);

public record ObjectiveDto(
    [property: JsonPropertyName("type")] ObjectiveType Type);

public record ProductStateDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("brand")] string Brand,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("gender")] string Gender,
    [property: JsonPropertyName("days_in_stock")] int DaysInStock,
    [property: JsonPropertyName("total_stock")] int TotalStock,
    [property: JsonPropertyName("sales_90d_total")] int Sales90dTotal,
    [property: JsonPropertyName("margin_90d")] decimal Margin90d,
    [property: JsonPropertyName("return_rate_pct")] double ReturnRatePct,
    [property: JsonPropertyName("weeks_of_cover")] double? WeeksOfCover,
    [property: JsonPropertyName("variants")] List<VariantStateDto> Variants);

public record VariantStateDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("stock")] int Stock,
    [property: JsonPropertyName("sales_90d")] int Sales90d,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("cost")] decimal Cost,
    [property: JsonPropertyName("margin_pct")] double MarginPct,
    [property: JsonPropertyName("days_in_stock")] int DaysInStock,
    [property: JsonPropertyName("weeks_of_cover")] double? WeeksOfCover);

public record LiquidationAdviceDto(
    [property: JsonPropertyName("state")] LiquidationStateDto State,
    [property: JsonPropertyName("decisions")] List<AdviceDecisionDto> Decisions,
    [property: JsonPropertyName("input_tokens")] long InputTokens,
    [property: JsonPropertyName("output_tokens")] long OutputTokens);

public record AdviceDecisionDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] DecisionTargetType Type,
    [property: JsonPropertyName("ref")] string Ref,
    [property: JsonPropertyName("decision")] LiquidationDecision Decision,
    [property: JsonPropertyName("probability")] double Probability);
