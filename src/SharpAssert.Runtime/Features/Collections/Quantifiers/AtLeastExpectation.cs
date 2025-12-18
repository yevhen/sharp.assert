// ABOUTME: Quantifier expectation: at least N items must satisfy the inner expectation
// ABOUTME: Shows failures when too few match (why we didn't reach minimum)

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

public sealed class AtLeastExpectation<T> : Expectation
{
    readonly IReadOnlyList<T> items;
    readonly int minimumCount;
    readonly Func<T, Expectation> expectationFactory;

    internal AtLeastExpectation(IReadOnlyList<T> items, int minimumCount, Func<T, Expectation> expectationFactory)
    {
        this.items = items;
        this.minimumCount = minimumCount;
        this.expectationFactory = expectationFactory;
    }

    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var passes = new List<(int Index, EvaluationResult Result)>();
        var failures = new List<(int Index, EvaluationResult Result)>();

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
            else
                failures.Add((i, result));
        }

        if (passes.Count >= minimumCount)
            return ExpectationResults.Pass(context.Expression);

        return new CollectionQuantifierResult(
            context.Expression,
            $"at least {minimumCount}",
            items.Count,
            passes.Count,
            failures.Count,
            Passed: false,
            failures);
    }
}
