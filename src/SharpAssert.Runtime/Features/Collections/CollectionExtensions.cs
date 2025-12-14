// ABOUTME: Extension methods for collection ordering assertions
// ABOUTME: Provides IsInAscendingOrder/IsInDescendingOrder APIs for FluentAssertions migration

using System;
using System.Collections.Generic;

namespace SharpAssert.Features.Collections;

/// <summary>Provides ordering validation extensions for collections.</summary>
public static class CollectionExtensions
{
    /// <summary>Validates that a collection is in ascending order using the default comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <returns>An expectation that validates ascending order.</returns>
    /// <remarks>
    /// <para>
    /// Empty and single-element collections are considered sorted in both ascending and descending order.
    /// Equal consecutive elements are allowed (non-strict ordering).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var numbers = new[] { 1, 2, 3, 4 };
    /// Assert(numbers.IsInAscendingOrder());
    /// </code>
    /// </example>
    public static CollectionOrderingExpectation<T> IsInAscendingOrder<T>(this IEnumerable<T> collection)
    {
        return new CollectionOrderingExpectation<T>(collection, OrderDirection.Ascending);
    }

    /// <summary>Validates that a collection is in ascending order using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="comparer">The comparer to use for ordering validation.</param>
    /// <returns>An expectation that validates ascending order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is null.</exception>
    public static CollectionOrderingExpectation<T> IsInAscendingOrder<T>(
        this IEnumerable<T> collection,
        IComparer<T> comparer)
    {
        if (comparer == null)
            throw new ArgumentNullException(nameof(comparer));

        return new CollectionOrderingExpectation<T>(collection, OrderDirection.Ascending, comparer);
    }

    /// <summary>Validates that a collection is in descending order using the default comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <returns>An expectation that validates descending order.</returns>
    /// <remarks>
    /// <para>
    /// Empty and single-element collections are considered sorted in both ascending and descending order.
    /// Equal consecutive elements are allowed (non-strict ordering).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var numbers = new[] { 4, 3, 2, 1 };
    /// Assert(numbers.IsInDescendingOrder());
    /// </code>
    /// </example>
    public static CollectionOrderingExpectation<T> IsInDescendingOrder<T>(this IEnumerable<T> collection)
    {
        return new CollectionOrderingExpectation<T>(collection, OrderDirection.Descending);
    }

    /// <summary>Validates that a collection is in descending order using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="comparer">The comparer to use for ordering validation.</param>
    /// <returns>An expectation that validates descending order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="comparer"/> is null.</exception>
    public static CollectionOrderingExpectation<T> IsInDescendingOrder<T>(
        this IEnumerable<T> collection,
        IComparer<T> comparer)
    {
        if (comparer == null)
            throw new ArgumentNullException(nameof(comparer));

        return new CollectionOrderingExpectation<T>(collection, OrderDirection.Descending, comparer);
    }
}
