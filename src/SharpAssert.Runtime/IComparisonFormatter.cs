namespace SharpAssert;

interface IComparisonFormatter
{
    bool CanFormat(AssertionOperand left, AssertionOperand right) => CanFormat(left.Value, right.Value);
    ComparisonResult CreateComparison(AssertionOperand left, AssertionOperand right) =>
        CreateComparison(left.Value, right.Value);

    bool CanFormat(object? leftValue, object? rightValue);
    ComparisonResult CreateComparison(object? leftValue, object? rightValue);
}
