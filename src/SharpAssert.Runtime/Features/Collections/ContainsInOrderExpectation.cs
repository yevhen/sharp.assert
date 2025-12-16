// ABOUTME: Expectation for verifying elements appear in a collection in order (gaps allowed).
// ABOUTME: Enables ContainsInOrder extension method for FluentAssertions migration.

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections;

/// <summary>Validates that a collection contains expected elements in order (gaps allowed).</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class ContainsInOrderExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly IEnumerable<T> expected;
    readonly IEqualityComparer<T> comparer;

    internal ContainsInOrderExpectation(IEnumerable<T> collection, IEnumerable<T> expected, IEqualityComparer<T>? comparer = null)
    {
        this.collection = collection;
        this.expected = expected;
        this.comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var expectedList = expected.ToList();
        if (expectedList.Count == 0)
            return ExpectationResults.Pass(context.Expression);

        var collectionList = collection.ToList();
        var expectedIndex = 0;
        T? lastFoundElement = default;
        var foundFirst = false;

        foreach (var item in collectionList)
        {
            if (comparer.Equals(item, expectedList[expectedIndex]))
            {
                lastFoundElement = item;
                foundFirst = true;
                expectedIndex++;

                if (expectedIndex == expectedList.Count)
                    return ExpectationResults.Pass(context.Expression);
            }
        }

        var formattedExpected = FormatSequence(expectedList);
        var missingElement = expectedList[expectedIndex];

        if (!foundFirst)
            return ExpectationResults.Fail(context.Expression,
                $"Expected collection to contain {formattedExpected} in order, but element {ValueFormatter.Format(missingElement)} was not found.");

        return ExpectationResults.Fail(context.Expression,
            $"Expected collection to contain {formattedExpected} in order, but element {ValueFormatter.Format(missingElement)} was not found after {ValueFormatter.Format(lastFoundElement)}.");
    }

    static string FormatSequence(List<T> items)
    {
        var formatted = string.Join(", ", items.Select(x => ValueFormatter.Format(x)));
        return $"[{formatted}]";
    }
}
