using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class SequenceEqualDemos
{
    /// <summary>
    /// Demonstrates SequenceEqual() with unified diff display.
    /// </summary>
    public static void UnifiedDiffDisplay()
    {
        var actual = new[] { 1, 2, 3, 4, 5 };
        var expected = new[] { 1, 2, 9, 4, 5 };
        Assert(actual.SequenceEqual(expected));
    }

    /// <summary>
    /// Demonstrates SequenceEqual() with different length sequences.
    /// </summary>
    public static void DifferentLengths()
    {
        var actual = new[] { 1, 2, 3 };
        var expected = new[] { 1, 2, 3, 4, 5 };
        Assert(actual.SequenceEqual(expected));
    }

    /// <summary>
    /// Demonstrates SequenceEqual() with element-by-element comparison.
    /// </summary>
    public static void ElementByElement()
    {
        var actual = new[] { "apple", "banana", "cherry" };
        var expected = new[] { "apple", "orange", "cherry" };
        Assert(actual.SequenceEqual(expected));
    }

    /// <summary>
    /// Demonstrates SequenceEqual() with large sequences showing truncation.
    /// </summary>
    public static void LargeSequences()
    {
        var actual = Enumerable.Range(1, 50).ToArray();
        var expected = Enumerable.Range(1, 50).Select(x => x == 25 ? 999 : x).ToArray();
        Assert(actual.SequenceEqual(expected));
    }
}
