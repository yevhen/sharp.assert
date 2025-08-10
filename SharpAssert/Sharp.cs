using System.Runtime.CompilerServices;
using SharpAssert;

public static class Sharp
{
    /// <summary>Validates that a condition is true, throwing an exception with detailed error information if false.</summary>
    public static void Assert(
        bool condition,
        string? message = null,
        [CallerArgumentExpression("condition")] string? expr = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (condition)
            return;

        var context = new AssertionContext(expr ?? "condition", file ?? "unknown", line, message);
        var formattedMessage = AssertionFormatter.FormatAssertionFailure(context);

        throw new SharpAssertionException(formattedMessage);
    }
}