// ABOUTME: Expectation for deep object equivalency with configurable comparison rules.
// ABOUTME: Wraps Compare-Net-Objects to provide rich diagnostics on failure.

using KellermanSoftware.CompareNetObjects;
using SharpAssert.Features.Shared;

namespace SharpAssert;

public sealed class IsEquivalentToExpectation<T> : Expectation
{
    readonly T actual;
    readonly T expected;
    readonly EquivalencyConfig<T> config;

    internal IsEquivalentToExpectation(T actual, T expected, EquivalencyConfig<T> config)
    {
        this.actual = actual;
        this.expected = expected;
        this.config = config;
    }

    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var compareLogic = new CompareLogic(config.ComparisonConfig);
        var result = compareLogic.Compare(actual, expected);

        if (result.AreEqual)
            return ExpectationResults.Pass(context.Expression);

        var differences = FormatDifferences(result.Differences);
        return ExpectationResults.Fail(context.Expression, differences.ToArray());
    }

    static List<string> FormatDifferences(IList<Difference> differences)
    {
        var lines = new List<string>();

        if (differences.Count > 0)
        {
            lines.Add("Object differences:");
            foreach (var diff in differences.Take(20))
            {
                lines.Add($"  {diff.PropertyName}: expected {FormatValue(diff.Object2Value)}, got {FormatValue(diff.Object1Value)}");
            }

            if (differences.Count > 20)
                lines.Add($"  ... ({differences.Count - 20} more differences)");
        }

        return lines;
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);
}
