using SharpAssert.Features.Shared;

namespace SharpAssert.Features.SequenceEqual;

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
            lines.AddRange(LengthMismatch.Render());
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

record SequenceLengthMismatch(
    int ExpectedLength,
    int ActualLength,
    IReadOnlyList<object?> FirstPreview,
    IReadOnlyList<object?> SecondPreview)
{
    public IReadOnlyList<RenderedLine> Render()
    {
        return
        [
            new(0, "SequenceEqual failed: length mismatch"),
            new(0, $"Expected length: {ExpectedLength}"),
            new(0, $"Actual length:   {ActualLength}"),
            new(0, $"First:  {FormatCollection(FirstPreview)}"),
            new(0, $"Second: {FormatCollection(SecondPreview)}")
        ];
    }

    static string FormatCollection(IReadOnlyList<object?> items)
    {
        if (items.Count == 0)
            return "[]";
        return $"[{string.Join(", ", items.Select(ValueFormatter.Format))}]";
    }
}

record SequenceDiffLine(SequenceDiffOperation Operation, int Index, object? Value);

enum SequenceDiffOperation
{
    Context,
    Added,
    Removed
}
