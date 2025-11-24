using System.Collections.Generic;
using DiffPlex.DiffBuilder.Model;

namespace SharpAssert.Runtime.Comparison;

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

record DefaultComparisonResult(AssertionOperand LeftOperand, AssertionOperand RightOperand)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record NullableComparisonResult(
    AssertionOperand LeftOperand,
    AssertionOperand RightOperand,
    object? LeftValue,
    object? RightValue,
    bool LeftIsNull,
    bool RightIsNull,
    Type? LeftExpressionType,
    Type? RightExpressionType)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record StringComparisonResult(
    AssertionOperand LeftOperand,
    AssertionOperand RightOperand,
    string? LeftText,
    string? RightText,
    StringDiff Diff)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

abstract record StringDiff;

record InlineStringDiff(IReadOnlyList<DiffSegment> Segments) : StringDiff;

record MultilineStringDiff(IReadOnlyList<TextDiffLine> Lines) : StringDiff;

record DiffSegment(StringDiffOperation Operation, string Text);

record TextDiffLine(ChangeType Type, string Text);

enum StringDiffOperation
{
    Unchanged,
    Deleted,
    Inserted
}

record CollectionComparisonResult(
    AssertionOperand LeftOperand,
    AssertionOperand RightOperand,
    IReadOnlyList<object?> LeftPreview,
    IReadOnlyList<object?> RightPreview,
    CollectionMismatch? FirstDifference,
    CollectionLengthDelta? LengthDifference)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record CollectionMismatch(int Index, object? LeftValue, object? RightValue);

record CollectionLengthDelta(IReadOnlyList<object?>? Missing, IReadOnlyList<object?>? Extra);

record ObjectComparisonResult(
    AssertionOperand LeftOperand,
    AssertionOperand RightOperand,
    IReadOnlyList<ObjectDifference> Differences,
    int TruncatedCount)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record ObjectDifference(string Path, object? Expected, object? Actual);

record SequenceEqualComparisonResult(
    AssertionOperand LeftOperand,
    AssertionOperand RightOperand,
    bool HasComparer,
    SequenceLengthMismatch? LengthMismatch,
    IReadOnlyList<SequenceDiffLine>? DiffLines,
    bool DiffTruncated,
    string? Error = null)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override T Accept<T>(IComparisonResultVisitor<T> visitor) => visitor.Visit(this);
}

record SequenceLengthMismatch(int ExpectedLength, int ActualLength, IReadOnlyList<object?> FirstPreview, IReadOnlyList<object?> SecondPreview);

record SequenceDiffLine(SequenceDiffOperation Operation, int Index, object? Value);

enum SequenceDiffOperation
{
    Context,
    Added,
    Removed
}
