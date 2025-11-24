namespace SharpAssert;

class StringComparer : IOperandComparer
{
    public bool CanCompare(object? leftValue, object? rightValue) =>
        leftValue is string or null && rightValue is string or null;

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        return new StringComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(string)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(string)),
            leftValue as string,
            rightValue as string,
            StringDiffer.FormatDiff(leftValue as string, rightValue as string));
    }
}
