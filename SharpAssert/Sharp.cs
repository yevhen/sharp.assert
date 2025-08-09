using System.Runtime.CompilerServices;
using SharpAssert;

public static class Sharp
{
    /// <summary>Entry point users call in tests. Rewriter replaces this call.</summary>
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

    private static string FormatAssertionFailure(string? expr, string? file, int line)
    {
        var expressionPart = string.IsNullOrEmpty(expr) ? "false" : expr;
        var locationPart = string.IsNullOrEmpty(file) ? $"line {line}" : $"{file}:{line}";
        return $"Assertion failed: {expressionPart}  at {locationPart}";
    }
}