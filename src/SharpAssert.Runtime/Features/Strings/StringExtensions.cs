// ABOUTME: Extension methods for string expectations (wildcard patterns, occurrences).
// ABOUTME: Provides FluentAssertions-compatible API for SharpAssert.
namespace SharpAssert.Features.Strings;

/// <summary>Extension methods for string validation.</summary>
public static class StringExtensions
{
    /// <summary>Creates an expectation that the string matches a wildcard pattern.</summary>
    /// <param name="text">The string to validate.</param>
    /// <param name="pattern">Wildcard pattern (* matches any sequence, ? matches single character).</param>
    /// <returns>An expectation that passes when the text matches the pattern.</returns>
    /// <example>
    /// <code>
    /// Assert("hello world".Matches("hello *"));
    /// Assert("test.txt".Matches("*.txt"));
    /// </code>
    /// </example>
    public static StringWildcardExpectation Matches(this string text, string pattern)
    {
        return new StringWildcardExpectation(text, pattern, ignoreCase: false);
    }

    /// <summary>Creates an expectation that the string matches a wildcard pattern (case-insensitive).</summary>
    /// <param name="text">The string to validate.</param>
    /// <param name="pattern">Wildcard pattern (* matches any sequence, ? matches single character).</param>
    /// <returns>An expectation that passes when the text matches the pattern (ignoring case).</returns>
    /// <example>
    /// <code>
    /// Assert("HELLO WORLD".MatchesIgnoringCase("hello *"));
    /// Assert("Test.TXT".MatchesIgnoringCase("*.txt"));
    /// </code>
    /// </example>
    public static StringWildcardExpectation MatchesIgnoringCase(this string text, string pattern)
    {
        return new StringWildcardExpectation(text, pattern, ignoreCase: true);
    }

    /// <summary>Creates an expectation that a substring appears exactly the specified number of times.</summary>
    /// <param name="text">The string to search in.</param>
    /// <param name="substring">The substring to count.</param>
    /// <param name="count">Occurrence count constraint.</param>
    /// <returns>An expectation that passes when occurrence constraints are met.</returns>
    /// <example>
    /// <code>
    /// Assert("error at line 5, error at line 10".Contains("error", Occur.Exactly(2)));
    /// Assert("warn, warn, warn".Contains("warn", Occur.AtLeast(2)));
    /// Assert("info".Contains("info", Occur.AtMost(1)));
    /// </code>
    /// </example>
    public static StringOccurrenceExpectation Contains(this string text, string substring, OccurrenceConstraint count)
    {
        return new StringOccurrenceExpectation(text, substring, count.Times, count.AtLeast, count.AtMost);
    }

    /// <summary>Creates an expectation that a regex pattern matches a specific number of times.</summary>
    /// <param name="text">The string to search in.</param>
    /// <param name="pattern">The regex pattern to match.</param>
    /// <param name="count">Occurrence count constraint.</param>
    /// <returns>An expectation that passes when occurrence constraints are met.</returns>
    /// <example>
    /// <code>
    /// Assert("test123 and test456".MatchesRegex(@"test\d+", Occur.Exactly(2)));
    /// Assert("error: foo, error: bar".MatchesRegex(@"error:\s+\w+", Occur.AtLeast(2)));
    /// Assert("warn: foo".MatchesRegex(@"warn:\s+\w+", Occur.AtMost(1)));
    /// </code>
    /// </example>
    public static RegexOccurrenceExpectation MatchesRegex(this string text, string pattern, OccurrenceConstraint count)
    {
        return new RegexOccurrenceExpectation(text, pattern, count.Times, count.AtLeast, count.AtMost);
    }
}

/// <summary>Represents an occurrence count constraint for substring validation.</summary>
public abstract record OccurrenceConstraint(int? Times, int? AtLeast, int? AtMost);

/// <summary>Specifies an exact occurrence count.</summary>
public static class Occur
{
    /// <summary>Creates a constraint requiring exactly the specified number of occurrences.</summary>
    public static OccurrenceConstraint Exactly(int count) => new ExactConstraint(count);

    /// <summary>Creates a constraint requiring at least the specified number of occurrences.</summary>
    public static OccurrenceConstraint AtLeast(int count) => new MinConstraint(count);

    /// <summary>Creates a constraint allowing at most the specified number of occurrences.</summary>
    public static OccurrenceConstraint AtMost(int count) => new MaxConstraint(count);

    sealed record ExactConstraint(int Count) : OccurrenceConstraint(Count, null, null);
    sealed record MinConstraint(int Count) : OccurrenceConstraint(null, Count, null);
    sealed record MaxConstraint(int Count) : OccurrenceConstraint(null, null, Count);
}
