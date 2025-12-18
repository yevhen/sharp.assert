// ABOUTME: Quantifier expectation: exactly N items must satisfy the inner expectation
// ABOUTME: Shows failures when too few match, shows extra matches when too many match

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

public sealed class ExactlyExpectation<T> : Expectation
{
    readonly IReadOnlyList<T> items;
    readonly int expectedCount;
    readonly Func<T, Expectation> expectationFactory;

    internal ExactlyExpectation(IReadOnlyList<T> items, int expectedCount, Func<T, Expectation> expectationFactory)
    {
        this.items = items;
        this.expectedCount = expectedCount;
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

        if (passes.Count == expectedCount)
            return ExpectationResults.Pass(context.Expression);

        var relevant = passes.Count < expectedCount ? failures : passes;

        return new CollectionQuantifierResult(
            context.Expression,
            $"exactly {expectedCount}",
            items.Count,
            passes.Count,
            relevant.Count,
            Passed: false,
            relevant);
    }
}
