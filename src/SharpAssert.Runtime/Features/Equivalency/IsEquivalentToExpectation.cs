// ABOUTME: Expectation for deep object equivalency with configurable comparison rules.
// ABOUTME: Wraps Compare-Net-Objects to provide rich diagnostics on failure.

using KellermanSoftware.CompareNetObjects;
using SharpAssert.Features.Shared;

namespace SharpAssert;

/// <summary>
/// Validates that two objects are equivalent through deep structural comparison.
/// </summary>
/// <typeparam name="T">The type of objects being compared.</typeparam>
/// <remarks>
/// <para>
/// This expectation performs a deep comparison of object graphs, comparing all
/// properties and fields recursively. Unlike reference equality (==) or value equality
/// (Equals), this checks that objects have the same structure and values throughout
/// their entire object graph.
/// </para>
/// <para>
/// Comparison can be customized via <see cref="EquivalencyConfig{T}"/> to exclude
/// specific members, include only certain members, or ignore collection ordering.
/// </para>
/// <para>
/// Thread Safety: This type is thread-safe if the compared objects are immutable
/// or not modified during comparison. The comparison process itself is not thread-safe
/// if objects are mutated concurrently.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Evaluates whether the actual and expected objects are equivalent.
    /// </summary>
    /// <param name="context">Call-site context for diagnostics.</param>
    /// <returns>
    /// A pass result if objects are equivalent; otherwise, a fail result listing
    /// up to 20 differences between the objects.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Performance: Deep comparison can be expensive for large object graphs.
    /// Consider the cost when comparing objects with many nested properties or
    /// large collections.
    /// </para>
    /// <para>
    /// When comparison fails, the first 20 property differences are shown with
    /// property paths, expected values, and actual values. Additional differences
    /// are counted but not displayed to keep output readable.
    /// </para>
    /// </remarks>
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
