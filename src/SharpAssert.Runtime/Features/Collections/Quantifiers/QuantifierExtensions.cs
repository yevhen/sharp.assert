// ABOUTME: Extension methods providing the public API for collection quantifiers
// ABOUTME: Includes Each, Some, None, One, Exactly, AtLeast, AtMost

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
    /// <remarks>
    /// <para>Empty collections pass (vacuous truth - no items can fail).</para>
    /// <para>All items are evaluated for complete diagnostics.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.Each(x => x.IsEquivalentTo(expected)));
    /// Assert(matrix.Each(row => row.Each(cell => cell.IsPositive())));
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
    /// <remarks>
    /// <para>Empty collections pass (vacuous truth - no items can fail).</para>
    /// <para>All items are evaluated for complete diagnostics.</para>
    /// </remarks>
    /// <example>
    /// <code>
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

    /// <summary>
    /// Creates an expectation that at least one item satisfies the given expectation.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="expectation">Factory function that creates an expectation for each item.</param>
    /// <returns>An expectation that passes when at least one item satisfies the inner expectation.</returns>
    /// <remarks>
    /// <para>Empty collections fail (no items can satisfy).</para>
    /// <para>All items are evaluated to show why none matched on failure.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.Some(x => x.IsEquivalentTo(expected)));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.SomeExpectation<T> Some<T>(
        this IEnumerable<T> items,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.SomeExpectation<T>(items.ToList(), expectation);
    }

    /// <summary>
    /// Creates an expectation that at least one item satisfies the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="predicate">A predicate expression that at least one item must satisfy.</param>
    /// <returns>An expectation that passes when at least one item satisfies the predicate.</returns>
    /// <remarks>
    /// <para>Empty collections fail (no items can satisfy).</para>
    /// <para>All items are evaluated to show why none matched on failure.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.Some(x => x > 5));
    /// </code>
    /// </example>
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

    /// <summary>
    /// Creates an expectation that no items satisfy the given expectation.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="expectation">Factory function that creates an expectation for each item.</param>
    /// <returns>An expectation that passes when no items satisfy the inner expectation.</returns>
    /// <remarks>
    /// <para>Empty collections pass (vacuously - nothing can violate).</para>
    /// <para>Items that pass the inner expectation are shown as violations.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.None(x => x.IsNull()));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.NoneExpectation<T> None<T>(
        this IEnumerable<T> items,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.NoneExpectation<T>(items.ToList(), expectation);
    }

    /// <summary>
    /// Creates an expectation that no items satisfy the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="predicate">A predicate expression that no item must satisfy.</param>
    /// <returns>An expectation that passes when no items satisfy the predicate.</returns>
    /// <remarks>
    /// <para>Empty collections pass (vacuously - nothing can violate).</para>
    /// <para>Items that pass the predicate are shown as violations.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.None(x => x == null));
    /// </code>
    /// </example>
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

    /// <summary>
    /// Creates an expectation that exactly one item satisfies the given expectation.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="expectation">Factory function that creates an expectation for each item.</param>
    /// <returns>An expectation that passes when exactly one item satisfies the inner expectation.</returns>
    /// <remarks>
    /// <para>Empty collections fail (no items can satisfy).</para>
    /// <para>When zero match, shows all failures. When multiple match, shows extra matches.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.One(x => x.Id.Equals(targetId)));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.OneExpectation<T> One<T>(
        this IEnumerable<T> items,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.OneExpectation<T>(items.ToList(), expectation);
    }

    /// <summary>
    /// Creates an expectation that exactly one item satisfies the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="predicate">A predicate expression that exactly one item must satisfy.</param>
    /// <returns>An expectation that passes when exactly one item satisfies the predicate.</returns>
    /// <remarks>
    /// <para>Empty collections fail (no items can satisfy).</para>
    /// <para>When zero match, shows all failures. When multiple match, shows extra matches.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.One(x => x.Id == targetId));
    /// </code>
    /// </example>
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

    /// <summary>
    /// Creates an expectation that exactly N items satisfy the given expectation.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="count">The exact number of items that must satisfy the expectation.</param>
    /// <param name="expectation">Factory function that creates an expectation for each item.</param>
    /// <returns>An expectation that passes when exactly N items satisfy the inner expectation.</returns>
    /// <remarks>
    /// <para>Exactly(0) is equivalent to None(). Exactly(1) is equivalent to One().</para>
    /// <para>When too few match, shows failures. When too many match, shows extra matches.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.Exactly(3, x => x.IsActive()));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.ExactlyExpectation<T> Exactly<T>(
        this IEnumerable<T> items,
        int count,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.ExactlyExpectation<T>(items.ToList(), count, expectation);
    }

    /// <summary>
    /// Creates an expectation that exactly N items satisfy the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="count">The exact number of items that must satisfy the predicate.</param>
    /// <param name="predicate">A predicate expression that exactly N items must satisfy.</param>
    /// <returns>An expectation that passes when exactly N items satisfy the predicate.</returns>
    /// <remarks>
    /// <para>Exactly(0) is equivalent to None(). Exactly(1) is equivalent to One().</para>
    /// <para>When too few match, shows failures. When too many match, shows extra matches.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.Exactly(3, x => x.IsActive));
    /// </code>
    /// </example>
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

    /// <summary>
    /// Creates an expectation that at least N items satisfy the given expectation.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="count">The minimum number of items that must satisfy the expectation.</param>
    /// <param name="expectation">Factory function that creates an expectation for each item.</param>
    /// <returns>An expectation that passes when at least N items satisfy the inner expectation.</returns>
    /// <remarks>
    /// <para>AtLeast(0) always passes. AtLeast(1) is equivalent to Some().</para>
    /// <para>On failure, shows items that did not satisfy the expectation.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.AtLeast(2, x => x.Score.IsGreaterThan(90)));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.AtLeastExpectation<T> AtLeast<T>(
        this IEnumerable<T> items,
        int count,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.AtLeastExpectation<T>(items.ToList(), count, expectation);
    }

    /// <summary>
    /// Creates an expectation that at least N items satisfy the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="count">The minimum number of items that must satisfy the predicate.</param>
    /// <param name="predicate">A predicate expression that at least N items must satisfy.</param>
    /// <returns>An expectation that passes when at least N items satisfy the predicate.</returns>
    /// <remarks>
    /// <para>AtLeast(0) always passes. AtLeast(1) is equivalent to Some().</para>
    /// <para>On failure, shows items that did not satisfy the predicate.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.AtLeast(2, x => x.Score > 90));
    /// </code>
    /// </example>
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

    /// <summary>
    /// Creates an expectation that at most N items satisfy the given expectation.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="count">The maximum number of items that may satisfy the expectation.</param>
    /// <param name="expectation">Factory function that creates an expectation for each item.</param>
    /// <returns>An expectation that passes when at most N items satisfy the inner expectation.</returns>
    /// <remarks>
    /// <para>AtMost(0) is equivalent to None().</para>
    /// <para>On failure, shows items that satisfied the expectation (extra matches).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.AtMost(1, x => x.HasError()));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.AtMostExpectation<T> AtMost<T>(
        this IEnumerable<T> items,
        int count,
        Func<T, Expectation> expectation)
    {
        return new Features.Collections.Quantifiers.AtMostExpectation<T>(items.ToList(), count, expectation);
    }

    /// <summary>
    /// Creates an expectation that at most N items satisfy the given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="items">The collection to check.</param>
    /// <param name="count">The maximum number of items that may satisfy the predicate.</param>
    /// <param name="predicate">A predicate expression that at most N items may satisfy.</param>
    /// <returns>An expectation that passes when at most N items satisfy the predicate.</returns>
    /// <remarks>
    /// <para>AtMost(0) is equivalent to None().</para>
    /// <para>On failure, shows items that satisfied the predicate (extra matches).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Assert(items.AtMost(1, x => x.HasError));
    /// </code>
    /// </example>
    public static Features.Collections.Quantifiers.AtMostExpectation<T> AtMost<T>(
        this IEnumerable<T> items,
        int count,
        Expression<Func<T, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var predicateText = predicate.Body.ToString();

        return new Features.Collections.Quantifiers.AtMostExpectation<T>(
            items.ToList(),
            count,
            item => new Features.Collections.Quantifiers.PredicateExpectation<T>(item, compiled, predicateText));
    }
}
