// ABOUTME: EvaluationResult subtype for collection quantifier failures
// ABOUTME: Holds nested results with indices for rich diagnostic rendering

using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Collections.Quantifiers;

public sealed record CollectionQuantifierResult(
    string ExpressionText,
    string QuantifierName,
    int TotalCount,
    int PassCount,
    int FailCount,
    IReadOnlyList<(int Index, EvaluationResult Result)> Failures)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => FailCount == 0;

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
