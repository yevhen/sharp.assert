#pragma warning disable CS1591
using System.Linq.Expressions;

namespace SharpAssert;

public enum BinaryOp { Eq, Ne, Lt, Le, Gt, Ge }

public static class SharpInternal
{
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