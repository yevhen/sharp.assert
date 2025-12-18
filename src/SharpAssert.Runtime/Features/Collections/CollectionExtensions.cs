// ABOUTME: Extension methods for collection validation expectations (ordering, uniqueness, set operations).
// ABOUTME: Provides IsInAscendingOrder/IsInDescendingOrder, AllUnique, ContainsInOrder, subset/superset APIs.

using System;
using System.Collections.Generic;
using System.Linq;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections;

/// <summary>Provides validation extensions for collections including ordering, uniqueness, and set operations.</summary>
public static class CollectionExtensions
{
    /// <summary>Validates that a collection is in ascending order using the default comparer.</summary>
    public static Expectation IsInAscendingOrder<T>(this IEnumerable<T> collection) =>
        CheckOrdering(collection, OrderDirection.Ascending, Comparer<T>.Default);

    /// <summary>Validates that a collection is in ascending order using a custom comparer.</summary>
    public static Expectation IsInAscendingOrder<T>(this IEnumerable<T> collection, IComparer<T> comparer)
    {
        if (comparer == null) throw new ArgumentNullException(nameof(comparer));
        return CheckOrdering(collection, OrderDirection.Ascending, comparer);
    }

    /// <summary>Validates that a collection is in descending order using the default comparer.</summary>
    public static Expectation IsInDescendingOrder<T>(this IEnumerable<T> collection) =>
        CheckOrdering(collection, OrderDirection.Descending, Comparer<T>.Default);

    /// <summary>Validates that a collection is in descending order using a custom comparer.</summary>
    public static Expectation IsInDescendingOrder<T>(this IEnumerable<T> collection, IComparer<T> comparer)
    {
        if (comparer == null) throw new ArgumentNullException(nameof(comparer));
        return CheckOrdering(collection, OrderDirection.Descending, comparer);
    }

    static Expectation CheckOrdering<T>(IEnumerable<T> collection, OrderDirection direction, IComparer<T> comparer)
    {
        return Expectation.From(
            () =>
            {
                var items = collection.ToList();
                if (items.Count <= 1) return true;

                for (var i = 0; i < items.Count - 1; i++)
                {
                    var comparison = comparer.Compare(items[i], items[i + 1]);
                    var isViolation = direction == OrderDirection.Ascending ? comparison > 0 : comparison < 0;
                    if (isViolation) return false;
                }
                return true;
            },
            () =>
            {
                var items = collection.ToList();
                for (var i = 0; i < items.Count - 1; i++)
                {
                    var comparison = comparer.Compare(items[i], items[i + 1]);
                    var isViolation = direction == OrderDirection.Ascending ? comparison > 0 : comparison < 0;
                    if (isViolation)
                        return [$"Expected collection to be in {direction.ToString().ToLowerInvariant()} order, but found item at index {i} is in wrong order."];
                }
                return ["Ordering check failed."];
            }
        );
    }

    /// <summary>Validates that a collection contains only unique items.</summary>
    public static Expectation AllUnique<T>(this IEnumerable<T> collection) =>
        CheckUniqueness(collection, null, null);

    /// <summary>Validates that a collection contains only unique items by a projected key.</summary>
    public static Expectation AllUnique<T>(this IEnumerable<T> collection, Func<T, object?> keySelector) =>
        CheckUniqueness(collection, keySelector, null);

    /// <summary>Validates that a collection contains only unique items using a custom comparer.</summary>
    public static Expectation AllUnique<T>(this IEnumerable<T> collection, IEqualityComparer<object> comparer) =>
        CheckUniqueness(collection, null, comparer);

    static Expectation CheckUniqueness<T>(IEnumerable<T> collection, Func<T, object?>? keySelector, IEqualityComparer<object>? comparer)
    {
        return Expectation.From(
            () => FindDuplicates(collection, keySelector, comparer).Count == 0,
            () =>
            {
                var duplicates = FindDuplicates(collection, keySelector, comparer);
                if (duplicates.Count == 1)
                    return [$"Expected all items to be unique, but item {ValueFormatter.Format(duplicates[0])} is not unique."];
                var formatted = string.Join(", ", duplicates.Select(ValueFormatter.Format));
                return [$"Expected all items to be unique, but items {{{formatted}}} are not unique."];
            }
        );
    }

    static List<object> FindDuplicates<T>(IEnumerable<T> collection, Func<T, object?>? keySelector, IEqualityComparer<object>? comparer)
    {
        var items = collection.ToList();
        var seen = comparer != null ? new HashSet<object>(comparer) : new HashSet<object>();
        var duplicateKeys = new HashSet<object>(comparer ?? EqualityComparer<object>.Default);

        foreach (var item in items)
        {
            var key = keySelector != null ? keySelector(item) : item;
            if (key is null) continue;
            if (!seen.Add(key))
                duplicateKeys.Add(key);
        }

        return duplicateKeys.ToList();
    }

    /// <summary>Validates that a collection contains expected elements in order (gaps allowed).</summary>
    public static Expectation ContainsInOrder<T>(this IEnumerable<T> collection, IEnumerable<T> expected) =>
        CheckContainsInOrder(collection, expected, EqualityComparer<T>.Default);

    /// <summary>Validates that a collection contains expected elements in order using a custom comparer.</summary>
    public static Expectation ContainsInOrder<T>(this IEnumerable<T> collection, IEnumerable<T> expected, IEqualityComparer<T> comparer) =>
        CheckContainsInOrder(collection, expected, comparer);

    static Expectation CheckContainsInOrder<T>(IEnumerable<T> collection, IEnumerable<T> expected, IEqualityComparer<T> comparer)
    {
        return Expectation.From(
            () =>
            {
                var expectedList = expected.ToList();
                if (expectedList.Count == 0) return true;

                var expectedIndex = 0;
                foreach (var item in collection)
                {
                    if (comparer.Equals(item, expectedList[expectedIndex]))
                    {
                        expectedIndex++;
                        if (expectedIndex == expectedList.Count) return true;
                    }
                }
                return false;
            },
            () =>
            {
                var expectedList = expected.ToList();
                var expectedIndex = 0;
                T? lastFound = default;
                var foundFirst = false;

                foreach (var item in collection)
                {
                    if (comparer.Equals(item, expectedList[expectedIndex]))
                    {
                        lastFound = item;
                        foundFirst = true;
                        expectedIndex++;
                        if (expectedIndex == expectedList.Count) break;
                    }
                }

                var formattedExpected = FormatSequence(expectedList);
                var missing = expectedList[expectedIndex];

                if (!foundFirst)
                    return [$"Expected collection to contain {formattedExpected} in order, but element {ValueFormatter.Format(missing)} was not found."];

                return [$"Expected collection to contain {formattedExpected} in order, but element {ValueFormatter.Format(missing)} was not found after {ValueFormatter.Format(lastFound)}."];
            }
        );
    }

    /// <summary>Validates that a collection contains expected elements consecutively (no gaps).</summary>
    public static Expectation ContainsInConsecutiveOrder<T>(this IEnumerable<T> collection, IEnumerable<T> expected) =>
        CheckContainsInConsecutiveOrder(collection, expected, EqualityComparer<T>.Default);

    /// <summary>Validates that a collection contains expected elements consecutively using a custom comparer.</summary>
    public static Expectation ContainsInConsecutiveOrder<T>(this IEnumerable<T> collection, IEnumerable<T> expected, IEqualityComparer<T> comparer) =>
        CheckContainsInConsecutiveOrder(collection, expected, comparer);

    static Expectation CheckContainsInConsecutiveOrder<T>(IEnumerable<T> collection, IEnumerable<T> expected, IEqualityComparer<T> comparer)
    {
        return Expectation.From(
            () =>
            {
                var expectedList = expected.ToList();
                if (expectedList.Count == 0) return true;

                var collectionList = collection.ToList();
                for (var startIndex = 0; startIndex <= collectionList.Count - expectedList.Count; startIndex++)
                {
                    if (MatchesAt(collectionList, expectedList, startIndex, comparer))
                        return true;
                }
                return false;
            },
            () =>
            {
                var formattedExpected = FormatSequence(expected.ToList());
                return [$"Expected collection to contain {formattedExpected} in consecutive order, but sequence was not found."];
            }
        );
    }

    static bool MatchesAt<T>(List<T> collectionList, List<T> expectedList, int startIndex, IEqualityComparer<T> comparer)
    {
        for (var i = 0; i < expectedList.Count; i++)
        {
            if (!comparer.Equals(collectionList[startIndex + i], expectedList[i]))
                return false;
        }
        return true;
    }

    /// <summary>Validates that a collection is a subset of another collection.</summary>
    public static Expectation IsSubsetOf<T>(this IEnumerable<T> collection, IEnumerable<T> superset) =>
        CheckIsSubsetOf(collection, superset, EqualityComparer<T>.Default);

    /// <summary>Validates that a collection is a subset of another collection using a custom comparer.</summary>
    public static Expectation IsSubsetOf<T>(this IEnumerable<T> collection, IEnumerable<T> superset, IEqualityComparer<T> comparer) =>
        CheckIsSubsetOf(collection, superset, comparer);

    static Expectation CheckIsSubsetOf<T>(IEnumerable<T> collection, IEnumerable<T> superset, IEqualityComparer<T> comparer)
    {
        return Expectation.From(
            () =>
            {
                var supersetSet = new HashSet<T>(superset, comparer);
                return collection.All(item => supersetSet.Contains(item));
            },
            () =>
            {
                var supersetSet = new HashSet<T>(superset, comparer);
                var missing = collection.FirstOrDefault(item => !supersetSet.Contains(item));
                return [$"Expected collection to be a subset, but element {ValueFormatter.Format(missing)} is not in the superset."];
            }
        );
    }

    /// <summary>Validates that a collection is a superset of another collection.</summary>
    public static Expectation IsSupersetOf<T>(this IEnumerable<T> collection, IEnumerable<T> subset) =>
        CheckIsSupersetOf(collection, subset, EqualityComparer<T>.Default);

    /// <summary>Validates that a collection is a superset of another collection using a custom comparer.</summary>
    public static Expectation IsSupersetOf<T>(this IEnumerable<T> collection, IEnumerable<T> subset, IEqualityComparer<T> comparer) =>
        CheckIsSupersetOf(collection, subset, comparer);

    static Expectation CheckIsSupersetOf<T>(IEnumerable<T> collection, IEnumerable<T> subset, IEqualityComparer<T> comparer)
    {
        return Expectation.From(
            () =>
            {
                var collectionSet = new HashSet<T>(collection, comparer);
                return subset.All(item => collectionSet.Contains(item));
            },
            () =>
            {
                var collectionSet = new HashSet<T>(collection, comparer);
                var missing = subset.FirstOrDefault(item => !collectionSet.Contains(item));
                return [$"Expected collection to be a superset, but element {ValueFormatter.Format(missing)} from subset is missing."];
            }
        );
    }

    /// <summary>Validates that two collections have at least one common element.</summary>
    public static Expectation Intersects<T>(this IEnumerable<T> collection, IEnumerable<T> other) =>
        CheckIntersects(collection, other, EqualityComparer<T>.Default);

    /// <summary>Validates that two collections have at least one common element using a custom comparer.</summary>
    public static Expectation Intersects<T>(this IEnumerable<T> collection, IEnumerable<T> other, IEqualityComparer<T> comparer) =>
        CheckIntersects(collection, other, comparer);

    static Expectation CheckIntersects<T>(IEnumerable<T> collection, IEnumerable<T> other, IEqualityComparer<T> comparer)
    {
        return Expectation.From(
            () =>
            {
                var collectionSet = new HashSet<T>(collection, comparer);
                return other.Any(item => collectionSet.Contains(item));
            },
            () => ["Expected collections to have at least one common element, but no intersection found."]
        );
    }

    /// <summary>Validates that collection elements can satisfy predicates with unique 1:1 matching.</summary>
    public static SatisfiesExpectation<T> Satisfies<T>(this IEnumerable<T> collection, params Func<T, bool>[] predicates) =>
        new SatisfiesExpectation<T>(collection, predicates);

    static string FormatSequence<T>(List<T> items)
    {
        var formatted = string.Join(", ", items.Select(x => ValueFormatter.Format(x)));
        return $"[{formatted}]";
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
