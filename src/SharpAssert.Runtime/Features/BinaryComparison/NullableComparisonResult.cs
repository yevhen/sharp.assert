using SharpAssert.Features.Shared;

namespace SharpAssert.Features.BinaryComparison;

record NullableComparisonResult(
    AssertionOperand LeftOperand,
    AssertionOperand RightOperand,
    object? LeftValue,
    object? RightValue,
    bool LeftIsNull,
    bool RightIsNull,
    Type? LeftExpressionType,
    Type? RightExpressionType)
    : ComparisonResult(LeftOperand, RightOperand)
{
    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"Left:  {FormatNullableValue(LeftValue, LeftIsNull, LeftExpressionType)}"),
            new(0, $"Right: {FormatNullableValue(RightValue, RightIsNull, RightExpressionType)}")
        };
        return lines;
    }

    static string FormatNullableValue(object? value, bool isNull, Type? expressionType)
    {
        if (isNull)
            return "null";

        return expressionType is not null
            ? FormatWithType(value, expressionType)
            : ValueFormatter.Format(value);
    }

    public static string FormatWithType(object? value, Type expressionType)
    {
        if (value == null)
            return "null";

        if (IsNullableType(expressionType))
            return $"{ValueFormatter.Format(value)}";

        return ValueFormatter.Format(value);
    }

    static bool IsNullableType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
