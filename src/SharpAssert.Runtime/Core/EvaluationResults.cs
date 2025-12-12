using System.Linq.Expressions;
using System.Text;
using SharpAssert.Features.Shared;

namespace SharpAssert.Core;

enum LogicalOperator
{
    AndAlso,
    OrElse
}

enum UnaryOperator
{
    Not
}

public record AssertionEvaluationResult(AssertionContext Context, EvaluationResult Result)
    : EvaluationResult(Context.Expression)
{
    public bool Passed => Result.BooleanValue == true;
    public override bool? BooleanValue => Result.BooleanValue;
    public override IReadOnlyList<RenderedLine> Render() => Result.Render();

    public string Format(string indent = "  ")
    {
        if (Passed)
            return string.Empty;

        var sb = new StringBuilder(Context.FormatMessage());
        var lines = Result.Render();
        Append(sb, lines, indent, baseIndent: 1);

        return sb.ToString().TrimEnd();
    }

    static void Append(StringBuilder sb, IReadOnlyList<RenderedLine> lines, string indent, int baseIndent)
    {
        foreach (var line in lines)
        {
            var totalIndent = baseIndent + line.IndentLevel;
            for (var i = 0; i < totalIndent; i++)
                sb.Append(indent);
            sb.AppendLine(line.Text);
        }
    }
}

record LogicalEvaluationResult(
    string ExpressionText,
    LogicalOperator Operator,
    EvaluationResult Left,
    EvaluationResult? Right,
    bool Value,
    bool ShortCircuited,
    ExpressionType NodeType)
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

        var lines = new List<RenderedLine>
        {
            new(0, $"{label}: {childLines[0].Text}")
        };

        for (var i = 1; i < childLines.Count; i++)
            lines.Add(new RenderedLine(childLines[i].IndentLevel, childLines[i].Text));

        return lines;
    }

    string GetLogicalExplanation() => Operator switch
    {
        LogicalOperator.AndAlso when Value => "&&: Both operands were true",
        LogicalOperator.AndAlso when ShortCircuited => "&&: Left operand was false",
        LogicalOperator.AndAlso => "&&: Right operand was false",
        LogicalOperator.OrElse when Value && ShortCircuited => "||: Left operand was true",
        LogicalOperator.OrElse when Value => "||: Right operand was true",
        _ => "||: Both operands were false"
    };
}

record UnaryEvaluationResult(
    string ExpressionText,
    UnaryOperator Operator,
    EvaluationResult Operand,
    object? OperandValue,
    bool Value)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, ExpressionText));

        lines.AddRange(RenderLabeled(Operand, "Operand"));
        lines.Add(new RenderedLine(0, $"!: Operand was {FormatValue(OperandValue)}"));

        return lines;
    }

    IReadOnlyList<RenderedLine> RenderLabeled(EvaluationResult child, string label)
    {
        var childLines = child.Render();
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

    static string FormatValue(object? value) => ValueFormatter.Format(value);
}

record BinaryComparisonEvaluationResult(
    string ExpressionText,
    ExpressionType Operator,
    ComparisonResult Comparison,
    bool Value)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, ExpressionText));

        var comparisonLines = Comparison.Render();
        foreach (var line in comparisonLines)
            lines.Add(line with { IndentLevel = 1 + line.IndentLevel });

        return lines;
    }
}

record ValueEvaluationResult(string ExpressionText, object? Value, Type ValueType)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value as bool?;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var valueText = ValueFormatter.Format(Value);
        return new List<RenderedLine> { new(0, valueText) };
    }
}

/// <summary>
/// Represents an evaluation that already produced detail lines (e.g., LINQ/SequenceEqual).
/// </summary>
record FormattedEvaluationResult(string ExpressionText, bool Value, IReadOnlyList<string> Lines)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(ExpressionText))
            lines.Add(new RenderedLine(0, ExpressionText));

        foreach (var line in Lines)
            lines.Add(new RenderedLine(1, line));

        return lines;
    }
}

record MethodCallEvaluationResult(
    string ExpressionText,
    bool Value,
    IReadOnlyList<EvaluationResult> Arguments)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(ExpressionText))
            lines.Add(new RenderedLine(0, ExpressionText));

        for (var i = 0; i < Arguments.Count; i++)
        {
            var arg = Arguments[i];
            var argLines = arg.Render();
            if (argLines.Count == 0) continue;

            lines.Add(new RenderedLine(1, $"Argument[{i}]: {argLines[0].Text}"));

            for (var j = 1; j < argLines.Count; j++)
                lines.Add(new RenderedLine(1 + argLines[j].IndentLevel, argLines[j].Text));
        }

        lines.Add(new RenderedLine(0, $"Result: {FormatValue(Value)}"));

        return lines;
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);
}
