using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class StringComparisonDemos
{
    /// <summary>
    /// Demonstrates single-line string comparison with inline character-level diff.
    /// </summary>
    public static void SingleLineInlineDiff()
    {
        var actual = "hello world";
        var expected = "hallo world";
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates multiline string comparison with line-by-line diff.
    /// </summary>
    public static void MultilineLineDiff()
    {
        var actual = """
            Line 1: Introduction
            Line 2: Body content
            Line 3: Conclusion
            """;
        var expected = """
            Line 1: Introduction
            Line 2: Different content
            Line 3: Conclusion
            """;
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates null vs non-null string comparison.
    /// </summary>
    public static void NullVsString()
    {
        string? nullString = null;
        var nonNullString = "text";
        Assert(nullString == nonNullString);
    }

    /// <summary>
    /// Demonstrates empty string vs non-empty string comparison.
    /// </summary>
    public static void EmptyVsNonEmpty()
    {
        var empty = "";
        var nonEmpty = "text";
        Assert(empty == nonEmpty);
    }

    /// <summary>
    /// Demonstrates long string comparison showing truncation and diff highlighting.
    /// </summary>
    public static void LongStrings()
    {
        var actual = "The quick brown fox jumps over the lazy dog. This is a very long string that demonstrates how SharpAssert handles lengthy text comparisons with proper formatting and truncation when necessary.";
        var expected = "The quick brown fox jumps over the lazy cat. This is a very long string that demonstrates how SharpAssert handles lengthy text comparisons with proper formatting and truncation when necessary.";
        Assert(actual == expected);
    }
}
