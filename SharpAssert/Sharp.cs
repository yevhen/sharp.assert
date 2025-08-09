using System.Runtime.CompilerServices;
using SharpAssert;

public static class Sharp
{
    /// <summary>Validates that a condition is true, throwing an exception with detailed error information if false.</summary>
    public static void Assert(
        bool condition,
        [CallerArgumentExpression("condition")] string? expr = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        if (!condition)
        {
            var message = FormatAssertionFailure(expr, file, line);
            throw new SharpAssertionException(message);
        }
    }

    static string FormatAssertionFailure(string? expr, string? file, int line)
    {
        var expressionPart = string.IsNullOrEmpty(expr) ? "false" : expr;
        var locationPart = string.IsNullOrEmpty(file) ? $"line {line}" : $"{file}:{line}";
        return $"Assertion failed: {expressionPart}  at {locationPart}";
    }
}