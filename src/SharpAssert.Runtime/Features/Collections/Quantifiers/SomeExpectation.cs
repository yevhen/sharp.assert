// ABOUTME: Quantifier expectation: at least one item must satisfy the inner expectation
// ABOUTME: Evaluates ALL items for complete diagnostics showing why none matched

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

public sealed class SomeExpectation<T> : Expectation
{
    readonly IReadOnlyList<T> items;
    readonly Func<T, Expectation> expectationFactory;

    internal SomeExpectation(IReadOnlyList<T> items, Func<T, Expectation> expectationFactory)
    {
        this.items = items;
        this.expectationFactory = expectationFactory;
    }

    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var failures = new List<(int Index, EvaluationResult Result)>();
        var passCount = 0;

        for (var i = 0; i < items.Count; i++)
        {
            var elementContext = context with
            {
                Expression = $"{context.Expression}[{i}]"
            };

            var expectation = expectationFactory(items[i]);
            var result = expectation.Evaluate(elementContext);

            if (result.BooleanValue == true)
                passCount++;
            else
                failures.Add((i, result));
        }

        if (passCount > 0)
            return ExpectationResults.Pass(context.Expression);

        return new CollectionQuantifierResult(
            context.Expression,
            "some",
            items.Count,
            passCount,
            failures.Count,
            Passed: false,
            failures);
    }
}
