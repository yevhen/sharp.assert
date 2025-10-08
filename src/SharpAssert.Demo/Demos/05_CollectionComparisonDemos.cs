using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class CollectionComparisonDemos
{
    /// <summary>
    /// Demonstrates collection comparison showing the first mismatched element.
    /// </summary>
    public static void FirstMismatch()
    {
        var actual = new[] { 1, 2, 3 };
        var expected = new[] { 1, 2, 4 };
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates collection comparison showing missing elements.
    /// </summary>
    public static void MissingElements()
    {
        var actual = new[] { 1, 2 };
        var expected = new[] { 1, 2, 3 };
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates collection comparison showing extra elements.
    /// </summary>
    public static void ExtraElements()
    {
        var actual = new[] { 1, 2, 3 };
        var expected = new[] { 1, 2 };
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates empty collection vs non-empty collection comparison.
    /// </summary>
    public static void EmptyCollection()
    {
        var actual = Array.Empty<int>();
        var expected = new[] { 1 };
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates collections with different lengths showing counts.
    /// </summary>
    public static void DifferentLengths()
    {
        var actual = new[] { 1, 2, 3, 4, 5 };
        var expected = new[] { 1, 2 };
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates large collection comparison with preview truncation.
    /// </summary>
    public static void LargeCollections()
    {
        var actual = Enumerable.Range(1, 100).ToArray();
        var expected = Enumerable.Range(1, 100).Select(x => x == 50 ? 999 : x).ToArray();
        Assert(actual == expected);
    }
}
