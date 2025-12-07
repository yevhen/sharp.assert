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
}
