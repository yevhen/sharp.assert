using SharpAssert.Features.Shared;

namespace SharpAssert.Core;

record ComposedExpectationEvaluationResult(
    string ExpressionText,
    string Operator,
    EvaluationResult Left,
    EvaluationResult? Right,
    bool Value,
    bool ShortCircuited)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(ExpressionText))
            lines.Add(new RenderedLine(0, ExpressionText));

        lines.AddRange(RenderLabeled(Left, "Left"));

        if (ShortCircuited == false && Right is not null)
            lines.AddRange(RenderLabeled(Right, "Right"));

        lines.Add(new RenderedLine(0, GetLogicalExplanation()));

        return lines;
    }

    IReadOnlyList<RenderedLine> RenderLabeled(EvaluationResult child, string label)
    {
        var childLines = child.Render();
        if (childLines.Count == 0)
            return [];

        var headline = child.BooleanValue is { } boolValue
            ? ValueFormatter.Format(boolValue)
            : childLines[0].Text;

        var lines = new List<RenderedLine>
        {
            new(0, $"{label}: {headline}")
        };

        if (headline == childLines[0].Text)
        {
            for (var i = 1; i < childLines.Count; i++)
                lines.Add(new RenderedLine(childLines[i].IndentLevel, childLines[i].Text));

            return lines;
        }

        foreach (var line in childLines)
            lines.Add(line with { IndentLevel = 1 + line.IndentLevel });

        return lines;
    }

    string GetLogicalExplanation()
    {
        var leftFailed = Left.BooleanValue != true;
        var rightFailed = Right?.BooleanValue != true;

        return Operator switch
        {
            "AND" when leftFailed && rightFailed => "AND: Both operands were false",
            "AND" when leftFailed => "AND: Left operand was false",
            "AND" => "AND: Right operand was false",
            _ => "OR: Both operands were false"
        };
    }
}
