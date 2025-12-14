// ABOUTME: Expectation that validates regex pattern match occurrence counts in strings.
// ABOUTME: Supports exact count, minimum, and maximum constraints for regex matches.
using System.Text.RegularExpressions;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Strings;

/// <summary>Validates that a regex pattern matches a specific number of times in a string.</summary>
/// <remarks>
/// <para>Supports exact count (times), minimum (atLeast), and maximum (atMost) constraints.</para>
/// <para>All constraints are optional but at least one should be specified for meaningful validation.</para>
/// </remarks>
/// <example>
/// <code>
/// using static SharpAssert.Sharp;
///
/// Assert("test123 and test456".MatchesRegex(@"test\d+", Occur.Exactly(2)));
/// Assert("error: foo, error: bar".MatchesRegex(@"error:\s+\w+", Occur.AtLeast(2)));
/// Assert("warn: foo".MatchesRegex(@"warn:\s+\w+", Occur.AtMost(1)));
/// </code>
/// </example>
public sealed class RegexOccurrenceExpectation(
    string text,
    string pattern,
    int? times,
    int? atLeast,
    int? atMost) : Expectation
{
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var count = CountMatches(text, pattern);

        if (times.HasValue && count != times.Value)
            return ExpectationResults.Fail(context.Expression,
                $"Expected regex pattern \"{pattern}\" to match exactly {times.Value} time(s), but found {count} match(es).",
                $"Actual: \"{text}\"");

        if (atLeast.HasValue && count < atLeast.Value)
            return ExpectationResults.Fail(context.Expression,
                $"Expected regex pattern \"{pattern}\" to match at least {atLeast.Value} time(s), but found {count} match(es).",
                $"Actual: \"{text}\"");

        if (atMost.HasValue && count > atMost.Value)
            return ExpectationResults.Fail(context.Expression,
                $"Expected regex pattern \"{pattern}\" to match at most {atMost.Value} time(s), but found {count} match(es).",
                $"Actual: \"{text}\"");

        return ExpectationResults.Pass(context.Expression);
    }

    static int CountMatches(string text, string pattern)
    {
        var regex = new Regex(pattern, RegexOptions.None);
        var matches = regex.Matches(text);
        return matches.Count;
    }
}
