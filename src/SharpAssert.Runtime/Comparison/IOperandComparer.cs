namespace SharpAssert.Runtime.Comparison;

interface IOperandComparer
{
    bool CanCompare(AssertionOperand left, AssertionOperand right) => CanCompare(left.Value, right.Value);
    ComparisonResult CreateComparison(AssertionOperand left, AssertionOperand right) =>
        CreateComparison(left.Value, right.Value);

    bool CanCompare(object? leftValue, object? rightValue);
    ComparisonResult CreateComparison(object? leftValue, object? rightValue);
}
