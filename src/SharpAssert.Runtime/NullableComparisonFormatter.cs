namespace SharpAssert;

class NullableComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(AssertionOperand left, AssertionOperand right)
    {
        if (left.IsNullableValueType || right.IsNullableValueType)
            return true;

        return CanFormat(left.Value, right.Value);
    }

    public string FormatComparison(AssertionOperand left, AssertionOperand right)
    {
        if (left.IsNullableValueType || right.IsNullableValueType)
            return FormatWithTypes(left, right);

        return FormatComparison(left.Value, right.Value);
    }

    public bool CanFormat(object? leftValue, object? rightValue)
    {
        return IsNullableType(leftValue?.GetType()) || IsNullableType(rightValue?.GetType());
    }

    public string FormatComparison(object? leftValue, object? rightValue)
    {
        var leftDisplay = FormatRuntimeValue(leftValue);
        var rightDisplay = FormatRuntimeValue(rightValue);

        return $"  Left:  {leftDisplay}\n  Right: {rightDisplay}";
    }

    static string FormatWithTypes(AssertionOperand left, AssertionOperand right)
    {
        var leftDisplay = FormatWithType(left.Value, left.ExpressionType!);
        var rightDisplay = FormatWithType(right.Value, right.ExpressionType!);

        return $"  Left:  {leftDisplay}\n  Right: {rightDisplay}";
    }

    static string FormatWithType(object? value, Type expressionType)
    {
        if (!IsNullableType(expressionType))
            return FormatActualValue(value);

        return value == null
            ? "HasValue: false"
            : $"HasValue: true, Value: {FormatActualValue(value)}";
    }

    static string FormatRuntimeValue(object? value)
    {
        if (value == null)
            return "null";

        var type = value.GetType();
        if (IsNullableType(type))
        {
            var hasValueProperty = type.GetProperty("HasValue");
            var valueProperty = type.GetProperty("Value");

            if (hasValueProperty?.GetValue(value) is bool hasValue)
            {
                if (!hasValue)
                    return "HasValue: false";

                var actualValue = valueProperty?.GetValue(value);
                return $"HasValue: true, Value: {FormatActualValue(actualValue)}";
            }
        }

        return FormatActualValue(value);
    }

    static bool IsNullableType(Type? type)
    {
        if (type == null) return false;
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    static string FormatActualValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => value.ToString()!
    };
}