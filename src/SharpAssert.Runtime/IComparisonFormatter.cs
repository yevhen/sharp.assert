namespace SharpAssert;

interface IComparisonFormatter
{
    bool CanFormat(AssertionOperand left, AssertionOperand right) => CanFormat(left.Value, right.Value);
    string FormatComparison(AssertionOperand left, AssertionOperand right) => FormatComparison(left.Value, right.Value);

    bool CanFormat(object? leftValue, object? rightValue);
    string FormatComparison(object? leftValue, object? rightValue);
}