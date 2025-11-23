using System.Collections.Generic;
using DiffPlex.DiffBuilder.Model;

namespace SharpAssert;

interface IComparisonResultVisitor<out T>
{
    T Visit(DefaultComparisonResult result);
    T Visit(NullableComparisonResult result);
    T Visit(StringComparisonResult result);
    T Visit(CollectionComparisonResult result);
    T Visit(ObjectComparisonResult result);
    T Visit(SequenceEqualComparisonResult result);
}

abstract record ComparisonResult(AssertionOperand Left, AssertionOperand Right)
{
    public abstract T Accept<T>(IComparisonResultVisitor<T> visitor);
}

record DefaultComparisonResult(AssertionOperand Left, AssertionOperand Right) : ComparisonResult(Left, Right)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record NullableComparisonResult(
    AssertionOperand Left,
    AssertionOperand Right,
    string LeftDisplay,
    string RightDisplay)
    : ComparisonResult(Left, Right)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record StringComparisonResult(
    AssertionOperand Left,
    AssertionOperand Right,
    IReadOnlyList<string> Lines)
    : ComparisonResult(Left, Right)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record CollectionComparisonResult(
    AssertionOperand Left,
    AssertionOperand Right,
    string LeftPreview,
    string RightPreview,
    string? FirstDifference,
    string? LengthDifference)
    : ComparisonResult(Left, Right)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record ObjectComparisonResult(
    AssertionOperand Left,
    AssertionOperand Right,
    IReadOnlyList<string> Lines)
    : ComparisonResult(Left, Right)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record SequenceEqualComparisonResult(
    AssertionOperand Left,
    AssertionOperand Right,
    bool HasComparer,
    IReadOnlyList<string> Lines)
    : ComparisonResult(Left, Right)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}
