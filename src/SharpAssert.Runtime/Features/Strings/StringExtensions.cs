// ABOUTME: Extension methods for string expectations (wildcard patterns, occurrences).
// ABOUTME: Provides FluentAssertions-compatible API for SharpAssert.
using System.Text.RegularExpressions;

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
    public static Expectation Matches(this string text, string pattern) =>
        MatchesWildcard(text, pattern, ignoreCase: false);

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
    public static Expectation MatchesIgnoringCase(this string text, string pattern) =>
        MatchesWildcard(text, pattern, ignoreCase: true);

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
    public static Expectation Contains(this string text, string substring, OccurrenceConstraint count) =>
        Expectation.From(
            () => CheckOccurrence(CountSubstring(text, substring), count),
            () => FormatSubstringFailure(substring, text, CountSubstring(text, substring), count)
        );

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
    public static Expectation MatchesRegex(this string text, string pattern, OccurrenceConstraint count) =>
        Expectation.From(
            () => CheckOccurrence(CountRegexMatches(text, pattern), count),
            () => FormatRegexFailure(pattern, text, CountRegexMatches(text, pattern), count)
        );

    static Expectation MatchesWildcard(string text, string pattern, bool ignoreCase)
    {
        var options = ignoreCase
            ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            : RegexOptions.None;
        var regexPattern = ConvertWildcardToRegex(pattern);

        return Expectation.From(
            () => Regex.IsMatch(text, regexPattern, options | RegexOptions.Singleline),
            () => [$"Expected text to match pattern \"{pattern}\" but it did not.", $"Actual: \"{text}\""]
        );
    }

    static string ConvertWildcardToRegex(string wildcardPattern) =>
        "^" + Regex.Escape(wildcardPattern)
            .Replace("\\*", ".*", System.StringComparison.Ordinal)
            .Replace("\\?", ".", System.StringComparison.Ordinal) + "$";

    static int CountSubstring(string text, string substring)
    {
        if (string.IsNullOrEmpty(substring))
            return 0;

        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(substring, index, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }

    static int CountRegexMatches(string text, string pattern) =>
        new Regex(pattern, RegexOptions.None).Matches(text).Count;

    static bool CheckOccurrence(int actualCount, OccurrenceConstraint constraint)
    {
        if (constraint.Times.HasValue && actualCount != constraint.Times.Value)
            return false;
        if (constraint.AtLeast.HasValue && actualCount < constraint.AtLeast.Value)
            return false;
        if (constraint.AtMost.HasValue && actualCount > constraint.AtMost.Value)
            return false;
        return true;
    }

    static string[] FormatSubstringFailure(string substring, string text, int actualCount, OccurrenceConstraint constraint)
    {
        var expectation = constraint switch
        {
            _ when constraint.Times.HasValue => $"exactly {constraint.Times.Value}",
            _ when constraint.AtLeast.HasValue => $"at least {constraint.AtLeast.Value}",
            _ when constraint.AtMost.HasValue => $"at most {constraint.AtMost.Value}",
            _ => "unknown"
        };

        return [
            $"Expected substring \"{substring}\" to appear {expectation} time(s), but found {actualCount}.",
            $"Actual: \"{text}\""
        ];
    }

    static string[] FormatRegexFailure(string pattern, string text, int actualCount, OccurrenceConstraint constraint)
    {
        var expectation = constraint switch
        {
            _ when constraint.Times.HasValue => $"exactly {constraint.Times.Value}",
            _ when constraint.AtLeast.HasValue => $"at least {constraint.AtLeast.Value}",
            _ when constraint.AtMost.HasValue => $"at most {constraint.AtMost.Value}",
            _ => "unknown"
        };

        return [
            $"Expected regex pattern \"{pattern}\" to match {expectation} time(s), but found {actualCount} match(es).",
            $"Actual: \"{text}\""
        ];
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
