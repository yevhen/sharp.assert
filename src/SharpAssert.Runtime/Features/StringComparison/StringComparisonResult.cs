using System.Text;
using DiffPlex.DiffBuilder.Model;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.StringComparison;

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
            builder.Append(segment.Render());
        return builder.ToString();
    }

    static string RenderMultilineDiffLine(TextDiffLine line) => line.Render();
}

abstract record StringDiff;

record InlineStringDiff(IReadOnlyList<DiffSegment> Segments) : StringDiff;

record MultilineStringDiff(IReadOnlyList<TextDiffLine> Lines) : StringDiff;

record DiffSegment(StringDiffOperation Operation, string Text)
{
    public string Render() => Operation switch
    {
        StringDiffOperation.Deleted => $"[-{Text}]",
        StringDiffOperation.Inserted => $"[+{Text}]",
        _ => Text
    };
}

record TextDiffLine(ChangeType Type, string Text)
{
    public string Render() => Type switch
    {
        ChangeType.Deleted => $"- {Text}",
        ChangeType.Inserted => $"+ {Text}",
        ChangeType.Modified => $"~ {Text}",
        _ => Text
    };
}

enum StringDiffOperation
{
    Unchanged,
    Deleted,
    Inserted
}
