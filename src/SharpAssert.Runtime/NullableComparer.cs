namespace SharpAssert;

class NullableComparer : IOperandComparer
{
    public bool CanCompare(AssertionOperand left, AssertionOperand right)
    {
        if (left.IsNullableValueType || right.IsNullableValueType)
            return true;

        return CanCompare(left.Value, right.Value);
    }

    public ComparisonResult CreateComparison(AssertionOperand left, AssertionOperand right)
    {
        if (left.IsNullableValueType || right.IsNullableValueType)
            return CompareWithTypes(left, right);

        return CreateComparison(left.Value, right.Value);
    }

    public bool CanCompare(object? leftValue, object? rightValue)
    {
        return IsNullableType(leftValue?.GetType()) || IsNullableType(rightValue?.GetType());
    }

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        return new NullableComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(object)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
            leftValue,
            rightValue,
            leftValue == null,
            rightValue == null,
            leftValue?.GetType(),
            rightValue?.GetType());
    }

    static ComparisonResult CompareWithTypes(AssertionOperand left, AssertionOperand right)
    {
        return new NullableComparisonResult(
            left,
            right,
            left.Value,
            right.Value,
            left.Value == null,
            right.Value == null,
            left.ExpressionType,
            right.ExpressionType);
    }

    static bool IsNullableType(Type? type) => type is not null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
