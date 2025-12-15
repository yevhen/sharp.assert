// ABOUTME: Expectation that validates strings match wildcard patterns (* and ?).
// ABOUTME: Supports case-sensitive and case-insensitive matching.
using System.Text.RegularExpressions;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Strings;

/// <summary>Validates that a string matches a wildcard pattern.</summary>
/// <remarks>
/// <para>Supports wildcard characters: * (matches any sequence) and ? (matches single character).</para>
/// <para>Pattern matching is anchored (must match entire string).</para>
/// </remarks>
/// <example>
/// <code>
/// using static SharpAssert.Sharp;
///
/// Assert("hello world".Matches("hello *"));
/// Assert("test.txt".Matches("*.txt"));
/// Assert("HELLO".MatchesIgnoringCase("hello"));
/// </code>
/// </example>
public sealed class StringWildcardExpectation(string text, string pattern, bool ignoreCase) : Expectation
{
    /// <summary>
    /// Evaluates whether the text matches the wildcard pattern.
    /// </summary>
    /// <param name="context">Call-site context for diagnostics.</param>
    /// <returns>
    /// A pass result if the text matches the pattern; otherwise, a fail result
    /// showing the expected pattern and actual text.
    /// </returns>
    /// <remarks>
    /// Performance: Converts the wildcard pattern to a regex on each evaluation.
    /// For repeated checks with the same pattern, consider caching the compiled regex.
    /// </remarks>
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var options = ignoreCase
            ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            : RegexOptions.None;

        var regexPattern = ConvertWildcardToRegex(pattern);
        var isMatch = Regex.IsMatch(text, regexPattern, options | RegexOptions.Singleline);

        if (isMatch)
            return ExpectationResults.Pass(context.Expression);

        return ExpectationResults.Fail(context.Expression,
            $"Expected text to match pattern \"{pattern}\" but it did not.",
            $"Actual: \"{text}\"");
    }

    static string ConvertWildcardToRegex(string wildcardPattern)
    {
        return "^"
            + Regex.Escape(wildcardPattern)
                .Replace("\\*", ".*", System.StringComparison.Ordinal)
                .Replace("\\?", ".", System.StringComparison.Ordinal)
            + "$";
    }
}
