// ABOUTME: Expectations for set operations (subset, superset, intersection).
// ABOUTME: Enables IsSubsetOf, IsSupersetOf, Intersects for FluentAssertions migration.

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections;

/// <summary>Validates that a collection is a subset of another collection.</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class IsSubsetOfExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly IEnumerable<T> superset;
    readonly IEqualityComparer<T> comparer;

    internal IsSubsetOfExpectation(IEnumerable<T> collection, IEnumerable<T> superset, IEqualityComparer<T>? comparer = null)
    {
        this.collection = collection;
        this.superset = superset;
        this.comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var supersetSet = new HashSet<T>(superset, comparer);

        foreach (var item in collection)
        {
            if (!supersetSet.Contains(item))
                return ExpectationResults.Fail(context.Expression,
                    $"Expected collection to be a subset, but element {ValueFormatter.Format(item)} is not in the superset.");
        }

        return ExpectationResults.Pass(context.Expression);
    }
}

/// <summary>Validates that a collection is a superset of another collection.</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class IsSupersetOfExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly IEnumerable<T> subset;
    readonly IEqualityComparer<T> comparer;

    internal IsSupersetOfExpectation(IEnumerable<T> collection, IEnumerable<T> subset, IEqualityComparer<T>? comparer = null)
    {
        this.collection = collection;
        this.subset = subset;
        this.comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var collectionSet = new HashSet<T>(collection, comparer);

        foreach (var item in subset)
        {
            if (!collectionSet.Contains(item))
                return ExpectationResults.Fail(context.Expression,
                    $"Expected collection to be a superset, but element {ValueFormatter.Format(item)} from subset is missing.");
        }

        return ExpectationResults.Pass(context.Expression);
    }
}

/// <summary>Validates that two collections have at least one common element.</summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public sealed class IntersectsExpectation<T> : Expectation
{
    readonly IEnumerable<T> collection;
    readonly IEnumerable<T> other;
    readonly IEqualityComparer<T> comparer;

    internal IntersectsExpectation(IEnumerable<T> collection, IEnumerable<T> other, IEqualityComparer<T>? comparer = null)
    {
        this.collection = collection;
        this.other = other;
        this.comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var collectionSet = new HashSet<T>(collection, comparer);

        foreach (var item in other)
        {
            if (collectionSet.Contains(item))
                return ExpectationResults.Pass(context.Expression);
        }

        return ExpectationResults.Fail(context.Expression,
            "Expected collections to have at least one common element, but no intersection found.");
    }
}
