namespace SharpAssert;

interface IComparisonFormatter
{
    bool CanFormat(object? leftValue, object? rightValue);
    string FormatComparison(object? leftValue, object? rightValue);
}