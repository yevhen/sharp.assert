#pragma warning disable CS1591
using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Async;
using SharpAssert.Features.Dynamic;

namespace SharpAssert;

public enum BinaryOp { Eq, Ne, Lt, Le, Gt, Ge }

public static class SharpInternal
{
    public static void AssertValue(
        Expression<Func<AssertValue>> valueExpression,
        ExprNode exprNode,
        string exprString,
        string file,
        int line,
        string? message = null)
    {
        var context = new ExpectationContext(exprNode.Text, file, line, message, exprNode);

        if (TryUnwrapBoolExpression(valueExpression.Body, out var boolExpression))
        {
            AssertBool(boolExpression, exprNode, exprString, file, line, message);
            return;
        }

        if (TryUnwrapExpectationExpression(valueExpression.Body, out var expectationExpression))
        {
            var expectation = CreateExpectation(expectationExpression);
            AssertExpectation(expectation, context);
            return;
        }

        var value = valueExpression.Compile(preferInterpretation: true)();

        if (value.IsExpectation)
        {
            AssertExpectation(value.Expectation, context);
            return;
        }

        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        var assertionContext = new AssertionContext(exprNode.Text, file, line, message, exprNode);

        if (value.Condition)
            return;

        var analysis = new AssertionEvaluationResult(assertionContext, new ValueEvaluationResult(exprNode.Text, value.Condition, typeof(bool)));
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void Assert(
        Expression<Func<bool>> condition,
        ExprNode exprNode,
        string exprString,
        string file,
        int line,
        string? message = null)
    {
        AssertBool(condition, exprNode, exprString, file, line, message);
    }

    public static void Assert(
        IExpectation expectation,
        string expr,
        string file,
        int line,
        string? message = null)
    {
        var context = new ExpectationContext(expr, file, line, message, new ExprNode(expr));
        AssertExpectation(expectation, context);
    }

    public static async Task AssertAsync(
        Func<Task<bool>> conditionAsync,
        string expr,
        string file,
        int line)
    {
        var context = new AssertionContext(expr, file, line, null, new ExprNode(expr));
        var analysis = await AsyncExpressionAnalyzer.AnalyzeSimple(conditionAsync, context);

        if (analysis.Passed)
            return;

        throw new SharpAssertionException(analysis.Format(), analysis);
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
        var analysis = await AsyncExpressionAnalyzer.AnalyzeBinary(leftAsync, rightAsync, op, context);

        if (analysis.Passed)
            return;

        throw new SharpAssertionException(analysis.Format(), analysis);
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
        var analysis = DynamicExpressionAnalyzer.AnalyzeBinary(left, right, op, context);

        if (analysis.Passed)
            return;

        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertDynamic(
        Func<bool> condition,
        string expr,
        string file,
        int line)
    {
        var context = new AssertionContext(expr, file, line, null, new ExprNode(expr));
        var analysis = DynamicExpressionAnalyzer.Analyze(condition, context);

        if (analysis.Passed)
            return;

        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    static void AssertExpectation(IExpectation expectation, ExpectationContext context)
    {
        if (context.Message is not null && string.IsNullOrWhiteSpace(context.Message))
            throw new ArgumentException("Message must be either null or non-empty", "message");

        var assertionContext = new AssertionContext(context.Expression, context.File, context.Line, context.Message, context.ExprNode);
        var result = expectation.Evaluate(context);
        var analysis = new AssertionEvaluationResult(assertionContext, result);

        if (analysis.Passed)
            return;

        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    static void AssertBool(
        Expression<Func<bool>> condition,
        ExprNode exprNode,
        string exprString,
        string file,
        int line,
        string? message)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        var assertionContext = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var analysis = ExpressionAnalyzer.Analyze(condition, assertionContext);

        if (analysis.Passed)
            return;

        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    static bool TryUnwrapBoolExpression(Expression expression, out Expression<Func<bool>> result)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked, Operand: var operand, Method: var method } &&
            method is not null &&
            method.Name == "op_Implicit" &&
            method.GetParameters() is [{ ParameterType: var parameterType }] &&
            parameterType == typeof(bool))
        {
            result = Expression.Lambda<Func<bool>>(operand);
            return true;
        }

        result = null!;
        return false;
    }

    static bool TryUnwrapExpectationExpression(Expression expression, out Expression expectationExpression)
    {
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked, Operand: var operand, Method: var method } &&
            method is not null &&
            method.Name == "op_Implicit" &&
            method.GetParameters() is [{ ParameterType: var parameterType }] &&
            typeof(IExpectation).IsAssignableFrom(parameterType))
        {
            expectationExpression = operand;
            return true;
        }

        expectationExpression = null!;
        return false;
    }

    static IExpectation CreateExpectation(Expression expression)
    {
        var asExpectation = Expression.Convert(expression, typeof(IExpectation));
        var factory = Expression.Lambda<Func<IExpectation>>(asExpectation);
        return factory.Compile(preferInterpretation: true)();
    }

}
