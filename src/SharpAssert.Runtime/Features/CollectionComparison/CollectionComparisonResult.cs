using SharpAssert.Features.Shared;

namespace SharpAssert.Features.CollectionComparison;

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
            lines.AddRange(FirstDifference.Render());

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

record CollectionMismatch(int Index, object? LeftValue, object? RightValue)
{
    public IReadOnlyList<RenderedLine> Render() =>
        [new(0, $"First difference at index {Index}: expected {ValueFormatter.Format(LeftValue)}, got {ValueFormatter.Format(RightValue)}")];
}

record CollectionLengthDelta(IReadOnlyList<object?>? Missing, IReadOnlyList<object?>? Extra);
