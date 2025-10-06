using System.Linq.Expressions;

namespace SharpAssert;

/// <summary>Binary comparison operations for rewriter use.</summary>
public enum BinaryOp { Eq, Ne, Lt, Le, Gt, Ge }

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
    /// <param name="usePowerAssert">Force PowerAssert for all assertions</param>
    /// <param name="usePowerAssertForUnsupported">Use PowerAssert for unsupported features</param>
    public static void Assert(
        Expression<Func<bool>> condition,
        string expr,
        string file,
        int line,
        string? message = null,
        bool usePowerAssert = false)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (usePowerAssert)
        {
            UsePowerAssert(condition, message);
            return;
        }

        var context = new AssertionContext(expr, file, line, message);
        var failureMessage = ExpressionAnalyzer.AnalyzeFailure(condition, context);
        
        if (string.IsNullOrEmpty(failureMessage))
            return;
            
        throw new SharpAssertionException(failureMessage);
    }
    
    /// <summary>
    /// Validates an async boolean expression and provides basic failure information.
    /// </summary>
    /// <param name="conditionAsync">Async function that returns boolean to validate</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    public static async Task AssertAsync(
        Func<Task<bool>> conditionAsync,
        string expr,
        string file,
        int line)
    {
        var context = new AssertionContext(expr, file, line, null);
        var failureMessage = await AsyncExpressionAnalyzer.AnalyzeSimpleAsyncFailure(conditionAsync, context);

        if (!string.IsNullOrEmpty(failureMessage))
            throw new SharpAssertionException(failureMessage);
    }
    
    /// <summary>
    /// Validates an async binary comparison and provides detailed failure information.
    /// </summary>
    /// <param name="leftAsync">Async function that returns the left operand</param>
    /// <param name="rightAsync">Async function that returns the right operand</param>
    /// <param name="op">Binary comparison operator</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    public static async Task AssertAsyncBinary(
        Func<Task<object?>> leftAsync,
        Func<Task<object?>> rightAsync,
        BinaryOp op,
        string expr,
        string file,
        int line)
    {
        var context = new AssertionContext(expr, file, line, null);
        var failureMessage = await AsyncExpressionAnalyzer.AnalyzeAsyncBinaryFailure(leftAsync, rightAsync, op, context);

        if (!string.IsNullOrEmpty(failureMessage))
            throw new SharpAssertionException(failureMessage);
    }
    
    
    /// <summary>
    /// Validates a dynamic binary comparison and provides detailed failure information.
    /// </summary>
    /// <param name="left">Function that returns the left operand</param>
    /// <param name="right">Function that returns the right operand</param>
    /// <param name="op">Binary comparison operator</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    public static void AssertDynamicBinary(
        Func<object?> left,
        Func<object?> right,
        BinaryOp op,
        string expr,
        string file,
        int line)
    {
        var context = new AssertionContext(expr, file, line, null);
        var failureMessage = DynamicExpressionAnalyzer.AnalyzeDynamicBinaryFailure(left, right, op, context);

        if (!string.IsNullOrEmpty(failureMessage))
            throw new SharpAssertionException(failureMessage);
    }

    /// <summary>
    /// Validates a dynamic expression and provides basic failure information.
    /// </summary>
    /// <param name="condition">Function that returns boolean to validate</param>
    /// <param name="expr">Text representation of the expression</param>
    /// <param name="file">Source file where assertion occurred</param>
    /// <param name="line">Line number where assertion occurred</param>
    public static void AssertDynamic(
        Func<bool> condition,
        string expr,
        string file,
        int line)
    {
        var context = new AssertionContext(expr, file, line, null);
        var failureMessage = DynamicExpressionAnalyzer.AnalyzeSimpleDynamicFailure(condition, context);

        if (!string.IsNullOrEmpty(failureMessage))
            throw new SharpAssertionException(failureMessage);
    }

    static void UsePowerAssert(Expression<Func<bool>> condition, string? message)
    {
        try
        {
            PowerAssert.PAssert.IsTrue(condition);
        }
        catch (Exception ex)
        {
            var failureMessage = message is not null
                ? $"{message}\n{ex.Message}"
                : ex.Message;

            var finalMessage = failureMessage.Replace("IsTrue", "Assert");

            throw new SharpAssertionException(finalMessage);
        }
    }
}