// ABOUTME: Quantifier expectation: at most N items must satisfy the inner expectation
// ABOUTME: Shows extra matches when too many satisfy (violations of the maximum)

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

public sealed class AtMostExpectation<T> : Expectation
{
    readonly IReadOnlyList<T> items;
    readonly int maximumCount;
    readonly Func<T, Expectation> expectationFactory;

    internal AtMostExpectation(IReadOnlyList<T> items, int maximumCount, Func<T, Expectation> expectationFactory)
    {
        this.items = items;
        this.maximumCount = maximumCount;
        this.expectationFactory = expectationFactory;
    }

    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var passes = new List<(int Index, EvaluationResult Result)>();

        for (var i = 0; i < items.Count; i++)
        {
            var elementContext = context with
            {
                Expression = $"{context.Expression}[{i}]"
            };

            var expectation = expectationFactory(items[i]);
            var result = expectation.Evaluate(elementContext);

            if (result.BooleanValue == true)
                passes.Add((i, result));
        }

        if (passes.Count <= maximumCount)
            return ExpectationResults.Pass(context.Expression);

        return new CollectionQuantifierResult(
            context.Expression,
            $"at most {maximumCount}",
            items.Count,
            passes.Count,
            passes.Count,
            Passed: false,
            passes);
    }
}
