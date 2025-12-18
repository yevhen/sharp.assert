// ABOUTME: Extension methods providing the public API for collection quantifiers
// ABOUTME: Phase 1 includes Each() only, future phases add Some, None, One, etc.

using System.Linq.Expressions;

namespace SharpAssert;

public static class QuantifierExtensions
{
    /// <summary>
    /// Creates an expectation that all items satisfy the given expectation.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="expectation">Factory function that creates an expectation for each item.</param>
    /// <returns>An expectation that passes when all items satisfy the inner expectation.</returns>
    /// <example>
    /// <code>
    /// // With Expectation factory
    /// Assert(items.Each(x => x.IsEquivalentTo(expected)));
    ///
    /// // Nested collections
    /// Assert(matrix.Each(row => row.Each(cell => cell > 0)));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.EachExpectation<T> Each<T>(
        this IEnumerable<T> items,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.EachExpectation<T>(items.ToList(), expectation);
    }

    /// <summary>
    /// Creates an expectation that all items satisfy the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="predicate">A predicate expression that each item must satisfy.</param>
    /// <returns>An expectation that passes when all items satisfy the predicate.</returns>
    /// <example>
    /// <code>
    /// // With bool predicate
    /// Assert(items.Each(x => x > 5));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.EachExpectation<T> Each<T>(
        this IEnumerable<T> items,
        Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var predicateText = predicate.Body.ToString();

        return new Features.Collections.Quantifiers.EachExpectation<T>(
            items.ToList(),
            item => new Features.Collections.Quantifiers.PredicateExpectation<T>(item, compiled, predicateText));
    }

    public static Features.Collections.Quantifiers.SomeExpectation<T> Some<T>(
        this IEnumerable<T> items,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.SomeExpectation<T>(items.ToList(), expectation);
    }

    public static Features.Collections.Quantifiers.SomeExpectation<T> Some<T>(
        this IEnumerable<T> items,
        Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var predicateText = predicate.Body.ToString();

        return new Features.Collections.Quantifiers.SomeExpectation<T>(
            items.ToList(),
            item => new Features.Collections.Quantifiers.PredicateExpectation<T>(item, compiled, predicateText));
    }

    public static Features.Collections.Quantifiers.NoneExpectation<T> None<T>(
        this IEnumerable<T> items,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.NoneExpectation<T>(items.ToList(), expectation);
    }

    public static Features.Collections.Quantifiers.NoneExpectation<T> None<T>(
        this IEnumerable<T> items,
        Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var predicateText = predicate.Body.ToString();

        return new Features.Collections.Quantifiers.NoneExpectation<T>(
            items.ToList(),
            item => new Features.Collections.Quantifiers.PredicateExpectation<T>(item, compiled, predicateText));
    }

    public static Features.Collections.Quantifiers.OneExpectation<T> One<T>(
        this IEnumerable<T> items,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.OneExpectation<T>(items.ToList(), expectation);
    }

    public static Features.Collections.Quantifiers.OneExpectation<T> One<T>(
        this IEnumerable<T> items,
        Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var predicateText = predicate.Body.ToString();

        return new Features.Collections.Quantifiers.OneExpectation<T>(
            items.ToList(),
            item => new Features.Collections.Quantifiers.PredicateExpectation<T>(item, compiled, predicateText));
    }

    public static Features.Collections.Quantifiers.ExactlyExpectation<T> Exactly<T>(
        this IEnumerable<T> items,
        int count,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.ExactlyExpectation<T>(items.ToList(), count, expectation);
    }

    public static Features.Collections.Quantifiers.ExactlyExpectation<T> Exactly<T>(
        this IEnumerable<T> items,
        int count,
        Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var predicateText = predicate.Body.ToString();

        return new Features.Collections.Quantifiers.ExactlyExpectation<T>(
            items.ToList(),
            count,
            item => new Features.Collections.Quantifiers.PredicateExpectation<T>(item, compiled, predicateText));
    }

    public static Features.Collections.Quantifiers.AtLeastExpectation<T> AtLeast<T>(
        this IEnumerable<T> items,
        int count,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.AtLeastExpectation<T>(items.ToList(), count, expectation);
    }

    public static Features.Collections.Quantifiers.AtLeastExpectation<T> AtLeast<T>(
        this IEnumerable<T> items,
        int count,
        Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var predicateText = predicate.Body.ToString();

        return new Features.Collections.Quantifiers.AtLeastExpectation<T>(
            items.ToList(),
            count,
            item => new Features.Collections.Quantifiers.PredicateExpectation<T>(item, compiled, predicateText));
    }
}
