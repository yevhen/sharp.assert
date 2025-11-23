using System.Collections.Generic;
using System.Linq.Expressions;

namespace SharpAssert;

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
}

record ValueEvaluationResult(string ExpressionText, object? Value, Type ValueType)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value as bool?;
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Represents an evaluation that already produced detail lines (e.g., LINQ/SequenceEqual).
/// </summary>
record FormattedEvaluationResult(string ExpressionText, bool Value, IReadOnlyList<string> Lines)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;
    public override T Accept<T>(IEvaluationResultVisitor<T> visitor) => visitor.Visit(this);
}
