using System.Collections.Generic;
using System.Linq.Expressions;
using SharpAssert;
using SharpAssert.Runtime.Comparison;

namespace SharpAssert.Runtime.Evaluation;

enum LogicalOperator
{
    AndAlso,
    OrElse
}

enum UnaryOperator
{
    Not
}

record RenderedLine(int IndentLevel, string Text);

interface IEvaluationResultVisitor<out T>
{
    T Visit(AssertionEvaluationResult result);
    T Visit(LogicalEvaluationResult result);
    T Visit(UnaryEvaluationResult result);
    T Visit(BinaryComparisonEvaluationResult result);
    T Visit(ValueEvaluationResult result);
    T Visit(FormattedEvaluationResult result);
}

abstract record EvaluationResult(string ExpressionText)
{
    public virtual bool? BooleanValue => null;
    public abstract T Accept<T>(IEvaluationResultVisitor<T> visitor);
}

record AssertionEvaluationResult(AssertionContext Context, EvaluationResult Result)
    : EvaluationResult(Context.Expression)
{
    public bool Passed => Result.BooleanValue == true;
    public override bool? BooleanValue => Result.BooleanValue;
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);
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
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);

    public IReadOnlyList<RenderedLine> Render(Func<EvaluationResult, IReadOnlyList<RenderedLine>> renderChild)
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(ExpressionText))
            lines.Add(new RenderedLine(0, ExpressionText));

        lines.AddRange(RenderLabeled(renderChild, Left, "Left"));

        if (ShortCircuited == false && Right is not null)
            lines.AddRange(RenderLabeled(renderChild, Right, "Right"));

        lines.Add(new RenderedLine(0, GetLogicalExplanation()));

        return lines;
    }

    static IReadOnlyList<RenderedLine> RenderLabeled(
        Func<EvaluationResult, IReadOnlyList<RenderedLine>> renderChild,
        EvaluationResult child,
        string label)
    {
        var childLines = renderChild(child);
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
        LogicalOperator.AndAlso when ShortCircuited => "&&: Left operand was false",
        LogicalOperator.AndAlso => "&&: Right operand was false",
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
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);

    public IReadOnlyList<RenderedLine> Render(Func<EvaluationResult, IReadOnlyList<RenderedLine>> renderChild)
    {
        var lines = new List<RenderedLine>();

        if (ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, ExpressionText));

        lines.AddRange(RenderLabeled(renderChild, Operand, "Operand"));
        lines.Add(new RenderedLine(0, $"!: Operand was {FormatValue(OperandValue)}"));

        return lines;
    }

    static IReadOnlyList<RenderedLine> RenderLabeled(
        Func<EvaluationResult, IReadOnlyList<RenderedLine>> renderChild,
        EvaluationResult child,
        string label)
    {
        var childLines = renderChild(child);
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
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);

    public IReadOnlyList<RenderedLine> Render(Func<ComparisonResult, IReadOnlyList<RenderedLine>> renderComparison)
    {
        var lines = new List<RenderedLine>();

        if (ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, ExpressionText));

        var comparisonLines = renderComparison(Comparison);
        foreach (var line in comparisonLines)
            lines.Add(line with { IndentLevel = 1 + line.IndentLevel });

        return lines;
    }
}

record ValueEvaluationResult(string ExpressionText, object? Value, Type ValueType)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value as bool?;
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);

    public IReadOnlyList<RenderedLine> Render()
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
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);

    public IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(ExpressionText))
            lines.Add(new RenderedLine(0, ExpressionText));

        foreach (var line in Lines)
            lines.Add(new RenderedLine(1, line));

        return lines;
    }
}
