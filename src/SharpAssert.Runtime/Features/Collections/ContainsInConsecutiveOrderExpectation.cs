// ABOUTME: Expectation for verifying elements appear consecutively in a collection (no gaps).
// ABOUTME: Enables ContainsInConsecutiveOrder extension method for FluentAssertions migration.

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections;

/// <summary>Validates that a collection contains expected elements consecutively (no gaps).</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class ContainsInConsecutiveOrderExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly IEnumerable<T> expected;
    readonly IEqualityComparer<T> comparer;

    internal ContainsInConsecutiveOrderExpectation(IEnumerable<T> collection, IEnumerable<T> expected, IEqualityComparer<T>? comparer = null)
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

        for (var startIndex = 0; startIndex <= collectionList.Count - expectedList.Count; startIndex++)
        {
            if (MatchesAt(collectionList, expectedList, startIndex))
                return ExpectationResults.Pass(context.Expression);
        }

        var formattedExpected = FormatSequence(expectedList);
        return ExpectationResults.Fail(context.Expression,
            $"Expected collection to contain {formattedExpected} in consecutive order, but sequence was not found.");
    }

    bool MatchesAt(List<T> collectionList, List<T> expectedList, int startIndex)
    {
        for (var i = 0; i < expectedList.Count; i++)
        {
            if (!comparer.Equals(collectionList[startIndex + i], expectedList[i]))
                return false;
        }
        return true;
    }

    static string FormatSequence(List<T> items)
    {
        var formatted = string.Join(", ", items.Select(x => ValueFormatter.Format(x)));
        return $"[{formatted}]";
    }
}
