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
            var message = AssertionFormatter.FormatAssertionFailure(expr, file, line);
            throw new SharpAssertionException(message);
        }
    }
}