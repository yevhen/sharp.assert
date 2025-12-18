// ABOUTME: Quantifier expectation: no items must satisfy the inner expectation
// ABOUTME: Items that PASS the inner expectation are VIOLATIONS for None

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

public sealed class NoneExpectation<T> : Expectation
{
    readonly IReadOnlyList<T> items;
    readonly Func<T, Expectation> expectationFactory;

    internal NoneExpectation(IReadOnlyList<T> items, Func<T, Expectation> expectationFactory)
    {
        this.items = items;
        this.expectationFactory = expectationFactory;
    }

    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var violations = new List<(int Index, EvaluationResult Result)>();

        for (var i = 0; i < items.Count; i++)
        {
            var elementContext = context with
            {
                Expression = $"{context.Expression}[{i}]"
            };

            var expectation = expectationFactory(items[i]);
            var result = expectation.Evaluate(elementContext);

            if (result.BooleanValue == true)
                violations.Add((i, result));
        }

        if (violations.Count == 0)
            return ExpectationResults.Pass(context.Expression);

        return new CollectionQuantifierResult(
            context.Expression,
            "none",
            items.Count,
            violations.Count,
            violations.Count,
            Passed: false,
            violations);
    }
}
