// ABOUTME: Extension methods for collection validation expectations (ordering and uniqueness).
// ABOUTME: Provides IsInAscendingOrder/IsInDescendingOrder and AllUnique APIs for FluentAssertions migration.

using System;
using System.Collections.Generic;

namespace SharpAssert.Features.Collections;

/// <summary>Provides validation extensions for collections including ordering and uniqueness checks.</summary>
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

    /// <summary>Validates that a collection contains only unique items.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <returns>An expectation that validates uniqueness.</returns>
    public static CollectionUniquenessExpectation<T> AllUnique<T>(this IEnumerable<T> collection)
    {
        return new CollectionUniquenessExpectation<T>(collection, null, null);
    }

    /// <summary>Validates that a collection contains only unique items by a projected key.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="keySelector">Key selector for uniqueness comparison.</param>
    /// <returns>An expectation that validates uniqueness.</returns>
    public static CollectionUniquenessExpectation<T> AllUnique<T>(
        this IEnumerable<T> collection,
        Func<T, object?> keySelector)
    {
        return new CollectionUniquenessExpectation<T>(collection, keySelector, null);
    }

    /// <summary>Validates that a collection contains only unique items using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="comparer">Equality comparer.</param>
    /// <returns>An expectation that validates uniqueness.</returns>
    public static CollectionUniquenessExpectation<T> AllUnique<T>(
        this IEnumerable<T> collection,
        IEqualityComparer<object> comparer)
    {
        return new CollectionUniquenessExpectation<T>(collection, null, comparer);
    }

    /// <summary>Validates that a collection contains expected elements in order (gaps allowed).</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="expected">The expected elements in order.</param>
    /// <returns>An expectation that validates the elements appear in order.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <c>SequenceEqual</c>, this allows gaps between elements. For example,
    /// [1, 2, 3, 4, 5].ContainsInOrder([1, 3, 5]) passes because 1, 3, and 5 appear
    /// in that order even though there are elements between them.
    /// </para>
    /// </remarks>
    public static ContainsInOrderExpectation<T> ContainsInOrder<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> expected)
    {
        return new ContainsInOrderExpectation<T>(collection, expected);
    }

    /// <summary>Validates that a collection contains expected elements in order using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="expected">The expected elements in order.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>An expectation that validates the elements appear in order.</returns>
    public static ContainsInOrderExpectation<T> ContainsInOrder<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> expected,
        IEqualityComparer<T> comparer)
    {
        return new ContainsInOrderExpectation<T>(collection, expected, comparer);
    }

    /// <summary>Validates that a collection contains expected elements consecutively (no gaps).</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="expected">The expected elements in consecutive order.</param>
    /// <returns>An expectation that validates the elements appear consecutively.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="ContainsInOrder{T}(IEnumerable{T}, IEnumerable{T})"/>, this requires
    /// elements to be adjacent with no gaps. For example, [1, 2, 3, 4, 5].ContainsInConsecutiveOrder([2, 3, 4])
    /// passes, but [1, 2, 3, 4, 5].ContainsInConsecutiveOrder([1, 3, 5]) fails.
    /// </para>
    /// </remarks>
    public static ContainsInConsecutiveOrderExpectation<T> ContainsInConsecutiveOrder<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> expected)
    {
        return new ContainsInConsecutiveOrderExpectation<T>(collection, expected);
    }

    /// <summary>Validates that a collection contains expected elements consecutively using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="expected">The expected elements in consecutive order.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>An expectation that validates the elements appear consecutively.</returns>
    public static ContainsInConsecutiveOrderExpectation<T> ContainsInConsecutiveOrder<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> expected,
        IEqualityComparer<T> comparer)
    {
        return new ContainsInConsecutiveOrderExpectation<T>(collection, expected, comparer);
    }

    /// <summary>Validates that a collection is a subset of another collection.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="superset">The superset to check against.</param>
    /// <returns>An expectation that validates the subset relationship.</returns>
    public static IsSubsetOfExpectation<T> IsSubsetOf<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> superset)
    {
        return new IsSubsetOfExpectation<T>(collection, superset);
    }

    /// <summary>Validates that a collection is a subset of another collection using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="superset">The superset to check against.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>An expectation that validates the subset relationship.</returns>
    public static IsSubsetOfExpectation<T> IsSubsetOf<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> superset,
        IEqualityComparer<T> comparer)
    {
        return new IsSubsetOfExpectation<T>(collection, superset, comparer);
    }

    /// <summary>Validates that a collection is a superset of another collection.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="subset">The subset to check against.</param>
    /// <returns>An expectation that validates the superset relationship.</returns>
    public static IsSupersetOfExpectation<T> IsSupersetOf<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> subset)
    {
        return new IsSupersetOfExpectation<T>(collection, subset);
    }

    /// <summary>Validates that a collection is a superset of another collection using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="subset">The subset to check against.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>An expectation that validates the superset relationship.</returns>
    public static IsSupersetOfExpectation<T> IsSupersetOf<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> subset,
        IEqualityComparer<T> comparer)
    {
        return new IsSupersetOfExpectation<T>(collection, subset, comparer);
    }

    /// <summary>Validates that two collections have at least one common element.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="other">The other collection to check for intersection.</param>
    /// <returns>An expectation that validates the collections intersect.</returns>
    public static IntersectsExpectation<T> Intersects<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> other)
    {
        return new IntersectsExpectation<T>(collection, other);
    }

    /// <summary>Validates that two collections have at least one common element using a custom comparer.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="other">The other collection to check for intersection.</param>
    /// <param name="comparer">The equality comparer to use.</param>
    /// <returns>An expectation that validates the collections intersect.</returns>
    public static IntersectsExpectation<T> Intersects<T>(
        this IEnumerable<T> collection,
        IEnumerable<T> other,
        IEqualityComparer<T> comparer)
    {
        return new IntersectsExpectation<T>(collection, other, comparer);
    }

    /// <summary>Validates that collection elements can satisfy predicates with unique 1:1 matching.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="predicates">The predicates that must each be satisfied by a different element.</param>
    /// <returns>An expectation that validates the matching exists.</returns>
    /// <remarks>
    /// <para>
    /// Uses bipartite matching to find if each predicate can be assigned to a unique element.
    /// This is useful when you need to verify that a collection contains elements satisfying
    /// multiple criteria, where each element can only satisfy one criterion.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var users = new[] { alice, bob, charlie };
    /// Assert(users.Satisfies(
    ///     u => u.Age > 18,
    ///     u => u.IsAdmin,
    ///     u => u.HasPremium
    /// ));
    /// </code>
    /// </example>
    public static SatisfiesExpectation<T> Satisfies<T>(
        this IEnumerable<T> collection,
        params Func<T, bool>[] predicates)
    {
        return new SatisfiesExpectation<T>(collection, predicates);
    }
}
