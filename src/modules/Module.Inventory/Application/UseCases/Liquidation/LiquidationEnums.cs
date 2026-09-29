using System.Text.Json.Serialization;

namespace Module.Inventory.Application.UseCases.Liquidation;

[JsonConverter(typeof(SnakeCaseEnumConverter<MarginFloor>))]
public enum MarginFloor
{
    NoLoss,
    LossUpTo10,
    LossUpToFifteen,
    GetRidOfIt
}

/// <summary>
/// Which candidate set to build the state from. Each one stresses a different
/// inference axis: audience (default), season, or intended use.
/// </summary>
[JsonConverter(typeof(SnakeCaseEnumConverter<LiquidationDataset>))]
public enum LiquidationDataset
{
    /// <summary>Demographic axis: who the customer is.</summary>
    Default,

    /// <summary>Season axis: what time of year the garment is for.</summary>
    Seasonal,

    /// <summary>Use axis: what the garment is built to do.</summary>
    Workwear
}

[JsonConverter(typeof(SnakeCaseEnumConverter<ObjectiveType>))]
public enum ObjectiveType
{
    ReleaseCash,
    FreeUpSpace,
    RotateSlowStock,
    AttractCustomers
}

[JsonConverter(typeof(SnakeCaseEnumConverter<DecisionTargetType>))]
public enum DecisionTargetType
{
    Product,
    Variant
}

[JsonConverter(typeof(SnakeCaseEnumConverter<LiquidationDecision>))]
public enum LiquidationDecision
{
    Liquidate,
    Keep,
    HumanReview
}
