using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class LinqOperationDemos
{
    /// <summary>
    /// Demonstrates Contains() failure showing the collection contents.
    /// </summary>
    public static void ContainsFailure()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var missingItem = 10;
        Assert(items.Contains(missingItem));
    }

    /// <summary>
    /// Demonstrates Any() with predicate showing items that match.
    /// </summary>
    public static void AnyWithPredicate()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        Assert(items.Any(x => x > 10));
    }

    /// <summary>
    /// Demonstrates All() with predicate showing items that fail the predicate.
    /// </summary>
    public static void AllWithPredicate()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        Assert(items.All(x => x > 3));
    }

    /// <summary>
    /// Demonstrates LINQ operations on empty collections.
    /// </summary>
    public static void EmptyCollectionLinq()
    {
        var items = Array.Empty<int>();
        Assert(items.Any());
    }
}
