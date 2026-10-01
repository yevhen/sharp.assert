// ABOUTME: EvaluationResult subtype for collection quantifier failures
// ABOUTME: Holds nested results with indices for rich diagnostic rendering

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

/// <summary>Contains the counts and item diagnostics for a collection expectation.</summary>
/// <param name="ExpressionText">The source expression.</param>
/// <param name="QuantifierName">The required item count or quantifier name.</param>
/// <param name="TotalCount">The total number of items checked.</param>
/// <param name="PassCount">The number of items that satisfied the inner expectation.</param>
/// <param name="FailCount">The number of items included in the failure diagnostics.</param>
/// <param name="Passed">Whether the collection expectation passed.</param>
/// <param name="Failures">The item indices and results included in the diagnostics.</param>
/// <remarks>Keep the supplied results unchanged during rendering or concurrent reads.</remarks>
/// <example><code>
/// var result = new CollectionQuantifierResult("items", "each", 1, 0, 1, false,
///     [(0, ExpectationResults.Fail("items[0]", "Value is not positive"))]);
/// </code></example>
public sealed record CollectionQuantifierResult(
    string ExpressionText,
    string QuantifierName,
    int TotalCount,
    int PassCount,
    int FailCount,
    bool Passed,
    IReadOnlyList<(int Index, EvaluationResult Result)> Failures)
    : EvaluationResult(ExpressionText)
{
    /// <inheritdoc />
    public override bool? BooleanValue => Passed;

    /// <inheritdoc />
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        lines.Add(new RenderedLine(0, ValueFormatter.Format(BooleanValue)));
        lines.Add(new RenderedLine(0, $"Expected {QuantifierName} item to satisfy expectation, but {FailCount} of {TotalCount} failed:"));

        foreach (var (index, result) in Failures)
        {
            var childLines = result.Render();
            var headline = result.BooleanValue is { } boolValue
                ? ValueFormatter.Format(boolValue)
                : childLines[0].Text;

            lines.Add(new RenderedLine(0, $"[{index}]: {headline}"));

            if (headline == childLines[0].Text)
            {
                for (var i = 1; i < childLines.Count; i++)
                    lines.Add(new RenderedLine(1 + childLines[i].IndentLevel, childLines[i].Text));
            }
            else
            {
                foreach (var line in childLines)
                    lines.Add(new RenderedLine(1 + line.IndentLevel, line.Text));
            }
        }

        return lines;
    }
}
