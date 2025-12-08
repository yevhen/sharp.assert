using System.Globalization;

namespace SharpAssert.Features.Shared;

static class ValueFormatter
{
    public static string Format(object? value) => value switch
    {
        null => "null",
        EvaluationUnavailable unavailable => unavailable.ToString(),
        string s => $"\"{s}\"",
        DateTime dt => dt.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };
}
