using Common.Utilities;
using JevDotNet;
using JevDotNet.Models;
using Module.Inventory.Application.Abstraction;
using Module.Inventory.Application.UseCases.Liquidation;

namespace Module.Inventory.Infrastructure.Advisor;

public class LiquidationAdvisor(JevClient jev) : ILiquidationAdvisor
{
    private const double LiquidateThreshold = 0.7;
    private const double KeepThreshold = 0.4;

    public async Task<Result<LiquidationAdviceDto>> AdviseAsync(
        LiquidationStateDto state, CancellationToken cancellationToken = default)
    {
        var questions = BuildQuestions(state);

        JevResponse<IReadOnlyDictionary<string, JevNoul>> response;
        try
        {
            response = await jev.EvaluateNoulsAsync(state, questions, cancellationToken);
        }
        catch (Exception ex)
        {
            return new Error(ErrorCode.InternalError, $"Liquidation evaluation via Jev failed: {ex.Message}");
        }

        // Lookup id -> (type, stable ref): the product's InternalCode is the key back to a row.
        Dictionary<string, (DecisionTargetType Type, string Ref)> refs = new(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < state.Products.Count; i++)
            refs[$"p{i}"] = (DecisionTargetType.Product, state.Products[i].Code);

        var decisions = questions.Select(q =>
        {
            var probability = response.Result[q.Id].Probability;
            var (type, @ref) = refs[q.Id];
            return new AdviceDecisionDto(q.Id, type, @ref, MapDecision(probability), probability);
        }).ToList();

        return new LiquidationAdviceDto(state, decisions, response.InputTokenCount, response.OutputTokenCount);
    }

    /// <summary>
    /// One question per product (fan-out in a single request).
    ///
    /// Product-level only: stores execute a markdown on the whole product, all sizes
    /// and colours together, because discounting a single size breaks the size run and
    /// makes the rest unsellable. Variants stay in the state so the model can still
    /// see the size distribution, but they are not asked about.
    ///
    /// IDs follow the p{i} convention; the internal code travels in the state as the
    /// stable reference. NOTE: the paths below must match the JSON keys of the DTOs.
    ///
    /// The wording follows the objective: asking "should this be liquidated?" when the
    /// goal is to attract customers measures the wrong thing, because clearing stock
    /// the customers are about to buy works against the goal.
    /// </summary>
    public static List<JevNoulQuestion> BuildQuestions(LiquidationStateDto state)
    {
        var objective = state.Context.Objective?.Type;
        var (ask, whenTrue, whenFalse) = FramingFor(objective);

        List<JevNoulQuestion> questions = [];
        for (var i = 0; i < state.Products.Count; i++)
        {
            var p = state.Products[i];
            questions.Add(new JevNoulQuestion($"p{i}",
                $"According to `context`, {ask} `products[{i}]` ({p.Code} {p.Name}) as a whole?",
                whenTrue, whenFalse));
        }
        return questions;
    }

    /// <summary>
    /// Returns the verb phrase to use in the question plus the true/false descriptions,
    /// chosen from the stated objective.
    /// </summary>
    public static (string Ask, string WhenTrue, string WhenFalse) FramingFor(ObjectiveType? objective) =>
        objective switch
        {
            ObjectiveType.AttractCustomers => (
                "should a discount be applied to",
                "A discount would help attract buyers and is within the policy",
                "A discount would not help attract buyers, or it would break the policy"),

            ObjectiveType.FreeUpSpace => (
                "should it be liquidated to free up shelf space",
                "Clearing it is the right way to free up space",
                "It should be kept, or there is a better way to free up space"),

            ObjectiveType.RotateSlowStock => (
                "should it be discounted to move it before it gets any older",
                "Discounting it is the right way to move it before it ages further",
                "It should be kept, or a discount would not move it"),

            _ => (
                "should it be liquidated to raise cash",
                "It meets the policy and should be liquidated",
                "It does not meet the policy and should be kept")
        };

    public static LiquidationDecision MapDecision(double probability) =>
        probability >= LiquidateThreshold ? LiquidationDecision.Liquidate
        : probability <= KeepThreshold ? LiquidationDecision.Keep
        : LiquidationDecision.HumanReview;
}
