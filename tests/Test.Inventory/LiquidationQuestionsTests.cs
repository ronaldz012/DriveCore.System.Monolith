using JevDotNet.Models;
using Module.Inventory.Application.UseCases.Liquidation;
using Module.Inventory.Infrastructure.Advisor;

namespace Test.Inventory;

public class LiquidationQuestionsTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static LiquidationStateDto BuildState(ObjectiveType? objective = null)
        => LiquidationStateFixture.Sample(
            "Campero", TenantId.ToString(), 90, new DateTime(2026, 10, 9),
            objective, MarginFloor.LossUpTo10, "test brief");

    [Fact]
    public void BuildQuestions_ShouldAskOneQuestionPerProduct_AndNotPerVariant()
    {
        var state = BuildState();

        var questions = LiquidationAdvisor.BuildQuestions(state);

        // 15 products, 29 variants -> 15 questions, not 44.
        Assert.Equal(state.Products.Count, questions.Count);
        Assert.Equal(15, state.Products.Count);
        Assert.Equal(29, state.Products.Sum(p => p.Variants.Count));

        Assert.DoesNotContain(questions, q => q.Id.Contains("_v"));
        Assert.Equal(questions.Count, questions.Select(q => q.Id.ToLowerInvariant()).Distinct().Count());
    }

    [Fact]
    public void BuildQuestions_ShouldPointEachQuestionAtItsProductPath()
    {
        var state = BuildState();

        var questions = LiquidationAdvisor.BuildQuestions(state);

        Assert.Equal("p0", questions[0].Id);
        Assert.Contains("`products[0]`", questions[0].Instructions);
        Assert.Contains("ZAR-2", questions[0].Instructions);
        Assert.Contains("Denim Overshirt", questions[0].Instructions);

        var last = questions[^1];
        Assert.Equal("p14", last.Id);
        Assert.Contains("`products[14]`", last.Instructions);
        Assert.Contains("HMI-9", last.Instructions);
    }

    [Fact]
    public void BuildQuestions_ShouldUseDiscountFraming_ForAttractCustomers()
    {
        var questions = LiquidationAdvisor.BuildQuestions(BuildState(ObjectiveType.AttractCustomers));

        Assert.All(questions, q => Assert.Contains("should a discount be applied to", q.Instructions));
    }

    [Fact]
    public void FramingFor_ShouldChangeTheVerbPerObjective()
    {
        Assert.Contains("liquidated to raise cash", LiquidationAdvisor.FramingFor(ObjectiveType.ReleaseCash).Ask);
        Assert.Contains("free up shelf space", LiquidationAdvisor.FramingFor(ObjectiveType.FreeUpSpace).Ask);
        Assert.Contains("discount be applied", LiquidationAdvisor.FramingFor(ObjectiveType.AttractCustomers).Ask);
        Assert.Contains("before it gets any older", LiquidationAdvisor.FramingFor(ObjectiveType.RotateSlowStock).Ask);
        // no objective -> the cash default
        Assert.Contains("liquidated to raise cash", LiquidationAdvisor.FramingFor(null).Ask);
    }

    [Fact]
    public void EveryObjective_ShouldHaveItsOwnFraming()
    {
        var asks = Enum.GetValues<ObjectiveType>()
            .Select(o => LiquidationAdvisor.FramingFor(o).Ask)
            .ToList();

        Assert.Equal(asks.Count, asks.Distinct().Count());
    }

    [Fact]
    public void EveryDataset_ShouldCarryCategoryOnEveryProduct()
    {
        foreach (var dataset in Enum.GetValues<LiquidationDataset>())
        {
            var state = LiquidationStateFixtures.Sample(
                dataset, "Campero", "tenant", 90, new DateTime(2026, 10, 9),
                ObjectiveType.RotateSlowStock, MarginFloor.LossUpToFifteen, "notes");

            Assert.NotEmpty(state.Products);
            Assert.All(state.Products, p => Assert.False(string.IsNullOrWhiteSpace(p.Category)));
        }
    }

    [Fact]
    public void SeasonalDataset_ShouldHoldThreeJacketsOfWhichOneIsWinter()
    {
        var state = LiquidationStateFixtures.Sample(
            LiquidationDataset.Seasonal, "Campero", "tenant", 90, new DateTime(2026, 10, 9),
            ObjectiveType.RotateSlowStock, MarginFloor.LossUpToFifteen, "notes");

        var jackets = state.Products.Where(p => p.Category == "Outerwear").ToList();
        Assert.Equal(3, jackets.Count);
        Assert.Contains(jackets, j => j.Name.Contains("Polar"));
        Assert.Contains(jackets, j => j.Name.Contains("Denim"));
        Assert.Contains(jackets, j => j.Name.Contains("Bomber"));
    }

    [Fact]
    public void WorkwearDataset_ShouldHoldSameNounOppositeAdjective()
    {
        var state = LiquidationStateFixtures.Sample(
            LiquidationDataset.Workwear, "Campero", "tenant", 90, new DateTime(2026, 10, 9),
            ObjectiveType.RotateSlowStock, MarginFloor.LossUpToFifteen, "notes");

        Assert.Contains(state.Products, p => p.Name == "Canvas Work Jacket");
        Assert.Contains(state.Products, p => p.Name == "Suede Jacket");
        Assert.Contains(state.Products, p => p.Name == "Cargo Pant");
        Assert.Contains(state.Products, p => p.Name == "Chino");
    }

    [Theory]
    [InlineData(0.95, LiquidationDecision.Liquidate)]
    [InlineData(0.70, LiquidationDecision.Liquidate)]
    [InlineData(0.55, LiquidationDecision.HumanReview)]
    [InlineData(0.40, LiquidationDecision.Keep)]
    [InlineData(0.02, LiquidationDecision.Keep)]
    public void MapDecision_ShouldApplyBands(double probability, LiquidationDecision expected)
    {
        Assert.Equal(expected, LiquidationAdvisor.MapDecision(probability));
    }
}
