using System.Text;

namespace SharpAssert.Core;

/// <summary>
/// Captures the context of an assertion including source location and expression text.
/// </summary>
/// <param name="Expression">The source text of the asserted expression.</param>
/// <param name="File">The source file path where the assertion was made.</param>
/// <param name="Line">The line number where the assertion was made.</param>
/// <param name="Message">Optional custom message provided by the user.</param>
/// <param name="ExprNode">The expression tree metadata for formatting.</param>
/// <remarks>
/// This record is created automatically by the SharpAssert rewriter and provides
/// all the information needed to format assertion failure messages with accurate
/// source locations and expression details.
/// </remarks>
/// <example>
/// <code>
/// // When you write:
/// Assert(x > 10, "Value too small");
///
/// // The rewriter generates a context with:
/// // Expression: "x > 10"
/// // File: "MyTest.cs"
/// // Line: 42
/// // Message: "Value too small"
/// </code>
/// </example>
public record AssertionContext(string Expression, string File, int Line, string? Message, ExprNode ExprNode)
{
    /// <summary>
    /// Formats the complete assertion failure message including custom message, expression, and location.
    /// </summary>
    /// <returns>A formatted string showing why the assertion failed and where.</returns>
    /// <remarks>
    /// The format includes the custom message (if provided), the expression text,
    /// and the source location in a standardized format suitable for test output.
    /// </remarks>
    public string FormatMessage()
    {
        var sb = new StringBuilder();
        if (Message is not null)
            sb.AppendLine(Message);

        var locationPart = FormatLocation();
        sb.Append("Assertion failed: ");
        sb.Append(Expression);
        sb.Append("  at ");
        sb.AppendLine(locationPart);
        return sb.ToString();
    }

    /// <summary>
    /// Formats the source location as "file:line" for display in error messages.
    /// </summary>
    /// <returns>A string in the format "File.cs:123".</returns>
    public string FormatLocation() => $"{File}:{Line}";
}