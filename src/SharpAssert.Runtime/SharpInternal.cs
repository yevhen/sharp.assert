#pragma warning disable CS1591
using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Async;
using SharpAssert.Features.Dynamic;

namespace SharpAssert;

public enum BinaryOp { Eq, Ne, Lt, Le, Gt, Ge }

public static class SharpInternal
{
    public static void Assert(
        Expression<Func<bool>> condition,
        ExprNode exprNode,
        string exprString,
        string file,
        int line,
        string? message = null)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var analysis = ExpressionAnalyzer.Analyze(condition, context);

        if (analysis.Passed)
            return;

        var failureMessage = analysis.Format();
        throw new SharpAssertionException(failureMessage, analysis);
    }

    public static async Task AssertAsync(
        Func<Task<bool>> conditionAsync,
        string expr,
        string file,
        int line)
    {
        var context = new AssertionContext(expr, file, line, null, new ExprNode(expr));
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
        var context = new AssertionContext(expr, file, line, null, new ExprNode(expr));
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
        var context = new AssertionContext(expr, file, line, null, new ExprNode(expr));
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
        var context = new AssertionContext(expr, file, line, null, new ExprNode(expr));
        var failureMessage = DynamicExpressionAnalyzer.AnalyzeSimpleDynamicFailure(condition, context);

        if (!string.IsNullOrEmpty(failureMessage))
            throw new SharpAssertionException(failureMessage);
    }

}
