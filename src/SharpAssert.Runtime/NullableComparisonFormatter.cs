namespace SharpAssert;

class NullableComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(AssertionOperand left, AssertionOperand right)
    {
        if (left.IsNullableValueType || right.IsNullableValueType)
            return true;

        return CanFormat(left.Value, right.Value);
    }

    public ComparisonResult CreateComparison(AssertionOperand left, AssertionOperand right)
    {
        if (left.IsNullableValueType || right.IsNullableValueType)
            return FormatWithTypes(left, right);

        return CreateComparison(left.Value, right.Value);
    }

    public bool CanFormat(object? leftValue, object? rightValue)
    {
        return IsNullableType(leftValue?.GetType()) || IsNullableType(rightValue?.GetType());
    }

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        var leftDisplay = FormatRuntimeValue(leftValue);
        var rightDisplay = FormatRuntimeValue(rightValue);

        return new NullableComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(object)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
            leftDisplay,
            rightDisplay);
    }

    static ComparisonResult FormatWithTypes(AssertionOperand left, AssertionOperand right)
    {
        var leftDisplay = FormatWithType(left.Value, left.ExpressionType!);
        var rightDisplay = FormatWithType(right.Value, right.ExpressionType!);

        return new NullableComparisonResult(left, right, leftDisplay, rightDisplay);
    }

    static string FormatWithType(object? value, Type expressionType)
    {
        if (!IsNullableType(expressionType))
            return FormatActualValue(value);

        return value == null ? "null" : $"{FormatActualValue(value)}";
    }

    static string FormatRuntimeValue(object? value)
    {
        if (value == null)
            return "null";

        var type = value.GetType();
        if (!IsNullableType(type))
            return FormatActualValue(value);

        var hasValueProperty = type.GetProperty("HasValue");
        var valueProperty = type.GetProperty("Value");

        if (hasValueProperty?.GetValue(value) is not bool hasValue)
            return FormatActualValue(value);

        if (!hasValue)
            return "null";

        var actualValue = valueProperty?.GetValue(value);
        return $"{FormatActualValue(actualValue)}";

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
        DateTime dt => dt.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };
}
