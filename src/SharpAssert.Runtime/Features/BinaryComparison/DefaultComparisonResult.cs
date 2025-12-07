using SharpAssert.Features.Shared;

namespace SharpAssert.Features.BinaryComparison;

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
