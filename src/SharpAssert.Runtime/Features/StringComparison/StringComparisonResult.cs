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

        if (Diff is not null)
        {
            if (Diff is InlineStringDiff)
            {
                lines.Add(new RenderedLine(0, $"Left:  {FormatStringValue(LeftText)}"));
                lines.Add(new RenderedLine(0, $"Right: {FormatStringValue(RightText)}"));
            }
            else if (Diff is MultilineStringDiff)
            {
                lines.Add(new RenderedLine(0, "Left:"));
                foreach (var line in (LeftText ?? string.Empty).Split('\n'))
                    lines.Add(new RenderedLine(1, line));

                lines.Add(new RenderedLine(0, "Right:"));
                foreach (var line in (RightText ?? string.Empty).Split('\n'))
                    lines.Add(new RenderedLine(1, line));
            }
            
            lines.AddRange(Diff.Render());
        }

        return lines;
    }

    static string FormatStringValue(string? value) => value == null ? "null" : $"\"{value}\"";
}

abstract record StringDiff
{
    public abstract IReadOnlyList<RenderedLine> Render();
}

record InlineStringDiff(IReadOnlyList<DiffSegment> Segments) : StringDiff
{
    public override IReadOnlyList<RenderedLine> Render()
    {
        var builder = new StringBuilder();
        foreach (var segment in Segments)
            builder.Append(segment.Render());
        return [new RenderedLine(0, $"Diff: {builder}")];
    }
}

record MultilineStringDiff(IReadOnlyList<TextDiffLine> Lines) : StringDiff
{
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine> { new(0, "Diff:") };
        foreach (var diffLine in Lines)
        {
            var prefix = diffLine.Type switch
            {
                ChangeType.Inserted => "+ ",
                ChangeType.Deleted => "- ",
                _ => "  "
            };
            lines.Add(new RenderedLine(1, prefix + diffLine.Text));
        }
        return lines;
    }
}

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
