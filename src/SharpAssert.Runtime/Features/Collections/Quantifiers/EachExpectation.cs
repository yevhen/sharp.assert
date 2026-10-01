// ABOUTME: Core quantifier expectation: all items must satisfy the inner expectation
// ABOUTME: Evaluates ALL items for complete diagnostics (no short-circuit)

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

/// <summary>Checks that every item satisfies an expectation.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>Every item is checked. Empty collections pass. Use separate expectations for concurrent checks.</remarks>
/// <example><code>Assert(items.Each(item => item > 0));</code></example>
public sealed class EachExpectation<T> : Expectation
{
    readonly IReadOnlyList<T> items;
    readonly Func<T, Expectation> expectationFactory;

    internal EachExpectation(IReadOnlyList<T> items, Func<T, Expectation> expectationFactory)
    {
        this.items = items;
        this.expectationFactory = expectationFactory;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var failures = new List<(int Index, EvaluationResult Result)>();

        for (var i = 0; i < items.Count; i++)
        {
            var elementContext = context with
            {
                Expression = $"{context.Expression}[{i}]"
            };

            var expectation = expectationFactory(items[i]);
            var result = expectation.Evaluate(elementContext);

            if (result.BooleanValue != true)
                failures.Add((i, result));
        }

        if (failures.Count == 0)
            return ExpectationResults.Pass(context.Expression);

        return new CollectionQuantifierResult(
            context.Expression,
            "each",
            items.Count,
            items.Count - failures.Count,
            failures.Count,
            Passed: false,
            failures);
    }
}
