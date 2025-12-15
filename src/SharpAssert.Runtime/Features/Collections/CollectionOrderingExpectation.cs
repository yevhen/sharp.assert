// ABOUTME: Provides ordering validation expectations for collections
// ABOUTME: Supports ascending/descending order checks with custom comparers and key selectors

using System.Collections.Generic;
using System.Linq;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections;

/// <summary>Validates that a collection is in ascending or descending order.</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <remarks>
/// <para>
/// Empty and single-element collections are considered sorted in both ascending and descending order.
/// Equal consecutive elements are allowed (non-strict ordering).
/// </para>
/// </remarks>
public sealed class CollectionOrderingExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly OrderDirection direction;
    readonly IComparer<T>? comparer;

    /// <summary>Creates an ordering expectation for a collection.</summary>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="direction">The expected order direction (ascending or descending).</param>
    /// <param name="comparer">Optional custom comparer.</param>
    public CollectionOrderingExpectation(IEnumerable<T> collection, OrderDirection direction, IComparer<T>? comparer = null)
    {
        this.collection = collection;
        this.direction = direction;
        this.comparer = comparer ?? Comparer<T>.Default;
    }

    /// <summary>
    /// Evaluates whether the collection is in the expected order.
    /// </summary>
    /// <param name="context">Call-site context for diagnostics.</param>
    /// <returns>
    /// A pass result if the collection is correctly ordered; otherwise, a fail result
    /// with diagnostic information showing where the ordering violation occurred.
    /// </returns>
    /// <remarks>
    /// Performance: This method materializes the collection into a list if not already materialized.
    /// For large collections, consider the O(n) space overhead.
    /// </remarks>
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var items = collection.ToList();

        if (items.Count <= 1)
            return ExpectationResults.Pass(context.Expression);

        for (var i = 0; i < items.Count - 1; i++)
        {
            var comparison = comparer!.Compare(items[i], items[i + 1]);

            if (IsViolation(comparison))
                return ExpectationResults.Fail(
                    context.Expression,
                    FormatOrderingViolation(items, i));
        }

        return ExpectationResults.Pass(context.Expression);
    }

    bool IsViolation(int comparison)
    {
        return direction == OrderDirection.Ascending ? comparison > 0 : comparison < 0;
    }

    string FormatOrderingViolation(List<T> items, int violationIndex)
    {
        return $"Expected collection to be in {direction.ToString().ToLowerInvariant()} order, " +
               $"but found item at index {violationIndex} is in wrong order.";
    }
}

/// <summary>Defines the direction of ordering.</summary>
public enum OrderDirection
{
    /// <summary>Elements should be in ascending order.</summary>
    Ascending,
    /// <summary>Elements should be in descending order.</summary>
    Descending
}
