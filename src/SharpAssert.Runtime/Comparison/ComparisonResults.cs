using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiffPlex.DiffBuilder.Model;
using SharpAssert;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Comparison;

abstract record ComparisonResult(AssertionOperand Left, AssertionOperand Right)
{
    public abstract IReadOnlyList<RenderedLine> Render();
}

record DefaultComparisonResult(AssertionOperand LeftOperand, AssertionOperand RightOperand)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override IReadOnlyList<RenderedLine> Render()
    {
        return new List<RenderedLine>
        {
            new(0, $"Left:  {FormatValue(LeftOperand.Value)}"),
            new(0, $"Right: {FormatValue(RightOperand.Value)}")
        };
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);
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
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"Left:  {FormatNullableValue(LeftValue, LeftIsNull, LeftExpressionType)}"),
            new(0, $"Right: {FormatNullableValue(RightValue, RightIsNull, RightExpressionType)}")
        };
        return lines;
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

record StringComparisonResult(
    AssertionOperand LeftOperand,
    AssertionOperand RightOperand,
    string? LeftText,
    string? RightText,
    StringDiff Diff)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (Diff is InlineStringDiff inline)
        {
            lines.Add(new RenderedLine(0, $"Left:  {FormatStringValue(LeftText)}"));
            lines.Add(new RenderedLine(0, $"Right: {FormatStringValue(RightText)}"));
            var diffText = RenderInlineDiff(inline.Segments);
            lines.Add(new RenderedLine(0, $"Diff: {diffText}"));
            return lines;
        }

        if (Diff is MultilineStringDiff multi)
        {
            lines.Add(new RenderedLine(0, "Left:"));
            foreach (var line in (LeftText ?? string.Empty).Split('\n'))
                lines.Add(new RenderedLine(1, line));

            lines.Add(new RenderedLine(0, "Right:"));
            foreach (var line in (RightText ?? string.Empty).Split('\n'))
                lines.Add(new RenderedLine(1, line));

            lines.Add(new RenderedLine(0, "Diff:"));
            foreach (var diffLine in multi.Lines)
                lines.Add(new RenderedLine(1, RenderMultilineDiffLine(diffLine)));
        }

        return lines;
    }

    static string FormatStringValue(string? value) => value == null ? "null" : $"\"{value}\"";

    static string RenderInlineDiff(IReadOnlyList<DiffSegment> segments)
    {
        var builder = new StringBuilder();
        foreach (var segment in segments)
        {
            builder.Append(segment.Operation switch
            {
                StringDiffOperation.Deleted => $"[-{segment.Text}]",
                StringDiffOperation.Inserted => $"[+{segment.Text}]",
                _ => segment.Text
            });
        }
        return builder.ToString();
    }

    static string RenderMultilineDiffLine(TextDiffLine line) => line.Type switch
    {
        ChangeType.Unchanged => line.Text,
        ChangeType.Deleted => $"- {line.Text}",
        ChangeType.Inserted => $"+ {line.Text}",
        ChangeType.Modified => $"~ {line.Text}",
        _ => line.Text
    };
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
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"Left:  {FormatCollection(LeftPreview)}"),
            new(0, $"Right: {FormatCollection(RightPreview)}")
        };

        if (FirstDifference is not null)
            lines.Add(new RenderedLine(0,
                $"First difference at index {FirstDifference.Index}: expected {FormatValue(FirstDifference.LeftValue)}, got {FormatValue(FirstDifference.RightValue)}"));

        if (LengthDifference is not null)
        {
            if (LengthDifference.Extra is not null)
                lines.Add(new RenderedLine(0, $"Extra elements: {FormatCollection(LengthDifference.Extra)}"));
            if (LengthDifference.Missing is not null)
                lines.Add(new RenderedLine(0, $"Missing elements: {FormatCollection(LengthDifference.Missing)}"));
        }

        return lines;
    }

    static string FormatCollection(IReadOnlyList<object?> items)
    {
        if (items.Count == 0)
            return "[]";

        return $"[{string.Join(", ", items.Select(FormatValue))}]";
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);
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
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (Differences.Count == 0)
            return lines;

        lines.Add(new RenderedLine(0, "Property differences:"));

        foreach (var diff in Differences)
        {
            lines.Add(new RenderedLine(1,
                $"{diff.Path}: expected {FormatValue(diff.Expected)}, got {FormatValue(diff.Actual)}"));
        }

        if (TruncatedCount > 0)
            lines.Add(new RenderedLine(1, $"... ({TruncatedCount} more differences)"));

        return lines;
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);
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
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (Error is not null)
        {
            lines.Add(new RenderedLine(0, Error));
            return lines;
        }

        if (LengthMismatch is not null)
        {
            lines.Add(new RenderedLine(0, "SequenceEqual failed: length mismatch"));
            lines.Add(new RenderedLine(0, $"Expected length: {LengthMismatch.ExpectedLength}"));
            lines.Add(new RenderedLine(0, $"Actual length:   {LengthMismatch.ActualLength}"));
            lines.Add(new RenderedLine(0, $"First:  {FormatCollection(LengthMismatch.FirstPreview)}"));
            lines.Add(new RenderedLine(0, $"Second: {FormatCollection(LengthMismatch.SecondPreview)}"));
            return lines;
        }

        lines.Add(new RenderedLine(0, "SequenceEqual failed: sequences differ"));
        if (HasComparer)
            lines.Add(new RenderedLine(0, "(using custom comparer)"));

        lines.Add(new RenderedLine(0, "Unified diff:"));
        if (DiffLines is not null)
        {
            foreach (var diff in DiffLines)
                lines.Add(new RenderedLine(1, RenderSequenceDiffLine(diff)));
        }

        if (DiffTruncated)
            lines.Add(new RenderedLine(1, "... (diff truncated)"));

        return lines;
    }

    static string FormatCollection(IReadOnlyList<object?> items)
    {
        if (items.Count == 0)
            return "[]";

        return $"[{string.Join(", ", items.Select(FormatValue))}]";
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);

    static string RenderSequenceDiffLine(SequenceDiffLine diff) => diff.Operation switch
    {
        SequenceDiffOperation.Added => $"+[{diff.Index}] {FormatValue(diff.Value)}",
        SequenceDiffOperation.Removed => $"-[{diff.Index}] {FormatValue(diff.Value)}",
        _ => $" [{diff.Index}] {FormatValue(diff.Value)}"
    };
}

record SequenceLengthMismatch(int ExpectedLength, int ActualLength, IReadOnlyList<object?> FirstPreview, IReadOnlyList<object?> SecondPreview);

record SequenceDiffLine(SequenceDiffOperation Operation, int Index, object? Value);

enum SequenceDiffOperation
{
    Context,
    Added,
    Removed
}
