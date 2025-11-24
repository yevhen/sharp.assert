using System.Text;
using SharpAssert.Runtime.Comparison;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Formatting;

class StringEvaluationFormatter(string indent = "  ") : IEvaluationResultVisitor<IReadOnlyList<RenderedLine>>,
    IComparisonResultVisitor<IReadOnlyList<RenderedLine>>
{
    public string Format(AssertionEvaluationResult result)
    {
        if (result.Passed)
            return string.Empty;

        var sb = new StringBuilder();
        var context = result.Context;
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);

        if (context.Message is not null)
            sb.AppendLine(context.Message);

        sb.Append("Assertion failed: ");
        sb.Append(context.Expression);
        sb.Append("  at ");
        sb.AppendLine(locationPart);

        var lines = result.Result.Accept(this);
        AppendLines(sb, lines, baseIndent: 1);

        return sb.ToString().TrimEnd();
    }

    public IReadOnlyList<RenderedLine> Visit(AssertionEvaluationResult result) =>
        result.Result.Accept(this);

    public IReadOnlyList<RenderedLine> Visit(LogicalEvaluationResult result)
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(result.ExpressionText))
            lines.Add(new RenderedLine(0, result.ExpressionText));

        lines.AddRange(RenderLabeled(result.Left, "Left"));

        if (result is { ShortCircuited: false, Right: not null })
            lines.AddRange(RenderLabeled(result.Right, "Right"));

        lines.Add(new RenderedLine(0, GetLogicalExplanation(result)));

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(UnaryEvaluationResult result)
    {
        var lines = new List<RenderedLine>();

        if (result.ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, result.ExpressionText));

        lines.AddRange(RenderLabeled(result.Operand, "Operand"));
        lines.Add(new RenderedLine(0, $"!: Operand was {FormatValue(result.OperandValue)}"));

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(BinaryComparisonEvaluationResult result)
    {
        var lines = new List<RenderedLine>();

        if (result.ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, result.ExpressionText));

        var comparisonLines = result.Comparison.Accept(this);
        foreach (var line in comparisonLines)
            lines.Add(line with { IndentLevel = 1 + line.IndentLevel });

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(ValueEvaluationResult result)
    {
        var valueText = FormatValue(result.Value);
        return new List<RenderedLine> { new(0, valueText) };
    }

    public IReadOnlyList<RenderedLine> Visit(FormattedEvaluationResult result)
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(result.ExpressionText))
            lines.Add(new RenderedLine(0, result.ExpressionText));

        foreach (var line in result.Lines)
            lines.Add(new RenderedLine(1, line));

        return lines;
    }

    static string GetLogicalExplanation(LogicalEvaluationResult result) => result.Operator switch
    {
        LogicalOperator.AndAlso when result.ShortCircuited => "&&: Left operand was false",
        LogicalOperator.AndAlso => "&&: Right operand was false",
        _ => "||: Both operands were false"
    };

    IReadOnlyList<RenderedLine> RenderLabeled(EvaluationResult child, string label)
    {
        var childLines = child.Accept(this);
        if (childLines.Count == 0)
            return [];

        var lines = new List<RenderedLine>
        {
            new(0, $"{label}: {childLines[0].Text}")
        };

        for (var i = 1; i < childLines.Count; i++)
            lines.Add(new RenderedLine(childLines[i].IndentLevel, childLines[i].Text));

        return lines;
    }

    void AppendLines(StringBuilder sb, IReadOnlyList<RenderedLine> lines, int baseIndent)
    {
        foreach (var line in lines)
        {
            var totalIndent = baseIndent + line.IndentLevel;
            for (var i = 0; i < totalIndent; i++)
                sb.Append(indent);
            sb.AppendLine(line.Text);
        }
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);

    public IReadOnlyList<RenderedLine> Visit(DefaultComparisonResult result)
    {
        return new List<RenderedLine>
        {
            new(0, $"Left:  {FormatValue(result.LeftOperand.Value)}"),
            new(0, $"Right: {FormatValue(result.RightOperand.Value)}")
        };
    }

    public IReadOnlyList<RenderedLine> Visit(NullableComparisonResult result)
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"Left:  {FormatNullableValue(result.LeftValue, result.LeftIsNull, result.LeftExpressionType)}"),
            new(0, $"Right: {FormatNullableValue(result.RightValue, result.RightIsNull, result.RightExpressionType)}")
        };
        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(StringComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(CollectionComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(ObjectComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(SequenceEqualComparisonResult result)
    {
        return result.Render();
    }

    static string FormatNullableValue(object? value, bool isNull, Type? expressionType)
    {
        if (isNull)
            return "null";

        return expressionType is not null
            ? ValueFormatter.FormatWithType(value, expressionType)
            : ValueFormatter.Format(value);
    }

}
