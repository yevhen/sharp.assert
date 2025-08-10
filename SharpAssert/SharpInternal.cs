using System.Linq.Expressions;

namespace SharpAssert;

/// <summary>Advanced assertion methods with detailed error reporting.</summary>
public static class SharpInternal
{
    /// <summary>
    /// Validates a boolean expression and provides detailed failure information.
    /// </summary>
    /// <param name="condition">Boolean expression to validate</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    /// <param name="message">Optional custom error message</param>
    public static void Assert(
        Expression<Func<bool>> condition,
        string expr,
        string file,
        int line,
        string? message = null)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        var analyzer = new ExpressionAnalyzer();
        var context = new AssertionContext(expr, file, line, message);
        var failureMessage = analyzer.AnalyzeFailure(condition, context);
        
        if (string.IsNullOrEmpty(failureMessage))
            return;
            
        throw new SharpAssertionException(failureMessage);
    }
}