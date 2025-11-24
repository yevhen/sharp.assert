namespace SharpAssert.Runtime.Comparison;

class DefaultComparer : IOperandComparer
{
    public bool CanCompare(object? leftValue, object? rightValue) => true;

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        return new DefaultComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(object)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)));
    }
}
