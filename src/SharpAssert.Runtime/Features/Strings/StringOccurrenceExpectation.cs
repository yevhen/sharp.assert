// ABOUTME: Expectation that validates substring occurrence counts in strings.
// ABOUTME: Supports exact count, minimum, and maximum constraints.
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Strings;

/// <summary>Validates that a substring appears a specific number of times in a string.</summary>
/// <remarks>
/// <para>Supports exact count (times), minimum (atLeast), and maximum (atMost) constraints.</para>
/// <para>All constraints are optional but at least one should be specified for meaningful validation.</para>
/// </remarks>
/// <example>
/// <code>
/// using static SharpAssert.Sharp;
///
/// Assert("error at line 5, error at line 10".Contains("error", times: 2));
/// Assert("warn, warn, warn".Contains("warn", atLeast: 2));
/// Assert("info".Contains("info", atMost: 1));
/// </code>
/// </example>
public sealed class StringOccurrenceExpectation(
    string text,
    string substring,
    int? times,
    int? atLeast,
    int? atMost) : Expectation
{
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var count = CountOccurrences(text, substring);

        if (times.HasValue && count != times.Value)
            return ExpectationResults.Fail(context.Expression,
                $"Expected substring \"{substring}\" to appear exactly {times.Value} time(s), but found {count}.",
                $"Actual: \"{text}\"");

        if (atLeast.HasValue && count < atLeast.Value)
            return ExpectationResults.Fail(context.Expression,
                $"Expected substring \"{substring}\" to appear at least {atLeast.Value} time(s), but found {count}.",
                $"Actual: \"{text}\"");

        if (atMost.HasValue && count > atMost.Value)
            return ExpectationResults.Fail(context.Expression,
                $"Expected substring \"{substring}\" to appear at most {atMost.Value} time(s), but found {count}.",
                $"Actual: \"{text}\"");

        return ExpectationResults.Pass(context.Expression);
    }

    static int CountOccurrences(string text, string substring)
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
}
