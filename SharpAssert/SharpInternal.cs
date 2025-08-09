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
    public static void Assert(
        Expression<Func<bool>> condition,
        string expr,
        string file,
        int line)
    {
        var analyzer = new ExpressionAnalyzer();
        var failureMessage = analyzer.AnalyzeFailure(condition, expr, file, line);
        
        if (string.IsNullOrEmpty(failureMessage))
            return;
            
        throw new SharpAssertionException(failureMessage);
    }
}