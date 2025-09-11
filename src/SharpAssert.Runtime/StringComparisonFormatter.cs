namespace SharpAssert;

class StringComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(object? leftValue, object? rightValue) =>
        leftValue is string or null && rightValue is string or null;

    public string FormatComparison(object? leftValue, object? rightValue) =>
        StringDiffer.FormatDiff(leftValue as string, rightValue as string);
}