namespace SharpAssert;

class StringComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(object? leftValue, object? rightValue) =>
        leftValue is string or null && rightValue is string or null;

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        return new StringComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(string)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(string)),
            StringDiffer.FormatDiffLines(leftValue as string, rightValue as string));
    }
}
