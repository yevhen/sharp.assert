using SharpAssert.Features.Shared;

namespace SharpAssert.Features.ObjectComparison;

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
