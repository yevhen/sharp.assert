// ABOUTME: Quantifier expectation: exactly one item must satisfy the inner expectation
// ABOUTME: Shows failures when none match, shows extra matches when multiple match

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

public sealed class OneExpectation<T> : Expectation
{
    readonly IReadOnlyList<T> items;
    readonly Func<T, Expectation> expectationFactory;

    internal OneExpectation(IReadOnlyList<T> items, Func<T, Expectation> expectationFactory)
    {
        this.items = items;
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

        if (passes.Count == 1)
            return ExpectationResults.Pass(context.Expression);

        var relevant = passes.Count == 0 ? failures : passes;

        return new CollectionQuantifierResult(
            context.Expression,
            "one",
            items.Count,
            passes.Count,
            relevant.Count,
            Passed: false,
            relevant);
    }
}
