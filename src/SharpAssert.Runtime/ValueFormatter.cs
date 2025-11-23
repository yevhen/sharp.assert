using System.Globalization;

namespace SharpAssert;

static class ValueFormatter
{
    public static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        DateTime dt => dt.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };

    public static string FormatWithType(object? value, Type expressionType)
    {
        if (value == null)
            return "null";

        if (IsNullableType(expressionType))
            return $"{Format(value)}";

        return Format(value);
    }

    static bool IsNullableType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
