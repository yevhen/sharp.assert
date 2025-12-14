// ABOUTME: Expectation that validates a collection contains only unique items (no duplicates).
// ABOUTME: Supports key selectors and custom equality comparers for flexible uniqueness checks.

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections;

/// <summary>Validates that a collection contains only unique items.</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class CollectionUniquenessExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly Func<T, object?>? keySelector;
    readonly IEqualityComparer<object>? comparer;

    /// <summary>Creates a uniqueness expectation for a collection.</summary>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="keySelector">Optional key selector for uniqueness comparison.</param>
    /// <param name="comparer">Optional equality comparer.</param>
    public CollectionUniquenessExpectation(
        IEnumerable<T> collection,
        Func<T, object?>? keySelector,
        IEqualityComparer<object>? comparer)
    {
        this.collection = collection;
        this.keySelector = keySelector;
        this.comparer = comparer;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var items = collection.ToList();
        var seen = comparer != null ? new HashSet<object>(comparer) : new HashSet<object>();
        var duplicateKeys = new HashSet<object>(comparer ?? EqualityComparer<object>.Default);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var key = keySelector != null ? keySelector(item) : item;

            if (key is null)
                continue;

            if (!seen.Add(key))
                duplicateKeys.Add(key);
        }

        if (duplicateKeys.Count == 0)
            return ExpectationResults.Pass(context.Expression);

        return ExpectationResults.Fail(context.Expression, FormatDuplicates(duplicateKeys));
    }

    string FormatDuplicates(IEnumerable<object> duplicateKeys)
    {
        var duplicates = duplicateKeys.ToList();

        if (duplicates.Count == 1)
            return $"Expected all items to be unique, but item {ValueFormatter.Format(duplicates[0])} is not unique.";

        var formatted = string.Join(", ", duplicates.Select(ValueFormatter.Format));
        return $"Expected all items to be unique, but items {{{formatted}}} are not unique.";
    }
}
