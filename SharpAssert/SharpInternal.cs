using System.Linq.Expressions;

namespace SharpAssert;

/// <summary>Internal APIs that the rewriter targets - not for direct user consumption.</summary>
public static class SharpInternal
{
    /// <summary>
    /// Internal assertion method for expression tree analysis.
    /// Used by the rewriter for sync cases without await or dynamic.
    /// </summary>
    /// <param name="condition">Expression tree to analyze</param>
    /// <param name="expr">Original expression text</param>
    /// <param name="file">Source file path</param>
    /// <param name="line">Source line number</param>
    public static void Assert(
        Expression<Func<bool>> condition,
        string expr,
        string file,
        int line)
    {
        var analyzer = new ExpressionAnalyzer();
        var failureMessage = analyzer.AnalyzeFailure(condition, expr, file, line);
        
        if (!string.IsNullOrEmpty(failureMessage))
        {
            throw new SharpAssertionException(failureMessage);
        }
    }

}