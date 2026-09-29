using System.Text.Json;
using Module.Inventory.Application.UseCases.Liquidation;

namespace Test.Inventory;

public class SnakeCaseEnumConverterTests
{
    [Fact]
    public void ShouldBreakBeforeDigits()
    {
        var json = JsonSerializer.Serialize(MarginFloor.LossUpTo10);
        Assert.Equal("\"loss_up_to_10\"", json);
    }

    [Theory]
    [InlineData(ObjectiveType.AttractCustomers, "attract_customers")]
    [InlineData(ObjectiveType.ReleaseCash, "release_cash")]
    [InlineData(LiquidationDecision.HumanReview, "human_review")]
    [InlineData(DecisionTargetType.Product, "product")]
    public void ShouldSerializeEnumsAsSnakeCase<T>(T value, string expected) where T : struct, Enum
    {
        Assert.Equal($"\"{expected}\"", JsonSerializer.Serialize(value));
    }

    [Fact]
    public void ShouldReadLooseCasingAndMissingUnderscores()
    {
        var value = JsonSerializer.Deserialize<MarginFloor>("\"loss_up_to10\"");
        Assert.Equal(MarginFloor.LossUpTo10, value);
    }
}
