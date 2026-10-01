// ABOUTME: Quantifier expectation: exactly N items must satisfy the inner expectation
// ABOUTME: Shows failures when too few match, shows extra matches when too many match

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

/// <summary>Checks that exactly the specified number of items satisfy an expectation.</summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>Every item is checked. Use separate expectations for concurrent checks.</remarks>
/// <example><code>Assert(items.Exactly(2, item => item > 0));</code></example>
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

    /// <inheritdoc />
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
