#pragma warning disable CS1591
using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Async;
using SharpAssert.Features.Dynamic;
using SharpAssert.Features.LinqOperations;
using SharpAssert.Features.SequenceEqual;
using SharpAssert.Features.Shared;

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

    public static void AssertCaptured(
        int count,
        Func<CaptureSession, bool> evaluate,
        Func<CaptureSession, Expression<Func<bool>>> describeExpression,
        Func<ExprNode> describeSource,
        string file,
        int line,
        Func<string?>? messageFactory)
    {
        var session = new CaptureSession(count);
        var passed = evaluate(session);
        var message = messageFactory?.Invoke();
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", "message");

        if (passed)
            return;

        var exprNode = describeSource();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var analysis = ExpressionAnalyzer.AnalyzeCaptured(describeExpression(session), session, context);
        if (analysis.Passed)
            return;

        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertExpectationValue(
        Expectation expectation,
        ExprNode exprNode,
        string file,
        int line,
        string? message = null)
    {
        var context = new ExpectationContext(exprNode.Text, file, line, message, exprNode);
        AssertExpectation(expectation, context);
    }

    public static Sharp.ExceptionResult<T> RequireException<T>(
        Func<Sharp.ExceptionResult<T>> create,
        string expr,
        string file,
        int line,
        Func<string?>? messageFactory) where T : Exception
    {
        Sharp.ExceptionResult<T> result;
        try
        {
            result = create();
        }
        catch (SharpAssertionException error) when (error.Result is null)
        {
            throw ExceptionCheckFailure(expr, file, line, messageFactory, error.Message);
        }

        return RequireCapturedException(result, expr, file, line, messageFactory);
    }

    public static async Task<Sharp.ExceptionResult<T>> RequireExceptionAsync<T>(
        Func<Task<Sharp.ExceptionResult<T>>> create,
        string expr,
        string file,
        int line,
        Func<string?>? messageFactory) where T : Exception
    {
        Sharp.ExceptionResult<T> result;
        try
        {
            result = await create();
        }
        catch (SharpAssertionException error) when (error.Result is null)
        {
            throw ExceptionCheckFailure(expr, file, line, messageFactory, error.Message);
        }

        return RequireCapturedException(result, expr, file, line, messageFactory);
    }

    public static void AssertBoolean(bool condition, string expr, string file, int line, string? message = null)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (condition)
            return;

        var context = new AssertionContext(expr, file, line, message, new ExprNode(expr));
        var analysis = new AssertionEvaluationResult(context, new ValueEvaluationResult(expr, false, typeof(bool)));
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertMethodCall<TReceiver, TArgument>(
        TReceiver receiver,
        TArgument argument,
        Func<TReceiver, TArgument, bool> predicate,
        Func<ExprNode> describe,
        string file,
        int line,
        string? message = null)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (predicate(receiver, argument))
            return;

        var exprNode = describe();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var result = new MethodCallEvaluationResult(exprNode.Text, false,
            [new ValueEvaluationResult(exprNode.Arguments![0].Text, argument, typeof(TArgument))]);
        var analysis = new AssertionEvaluationResult(context, result);
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertAny<T>(
        IEnumerable<T> source,
        Func<ExprNode> describe,
        string file,
        int line,
        Func<string?>? messageFactory)
    {
        ArgumentNullException.ThrowIfNull(source);
        var passed = Enumerable.Any(source);
        var message = messageFactory?.Invoke();
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", "message");

        if (passed)
            return;

        var exprNode = describe();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var result = new FormattedEvaluationResult(exprNode.Text, false, ["Any failed: collection is empty"]);
        var analysis = new AssertionEvaluationResult(context, result);
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertAny<T>(
        IEnumerable<T> source,
        Func<T, bool> predicate,
        string predicateText,
        Func<ExprNode> describe,
        string file,
        int line,
        Func<string?>? messageFactory)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var preview = new List<object?>();
        var count = 0;
        var passed = Enumerable.Any(source, item =>
        {
            if (count < LinqOperationFormatter.CollectionPreviewLimit)
                preview.Add(item);
            count++;
            return predicate(item);
        });

        var message = messageFactory?.Invoke();
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", "message");

        if (passed)
            return;

        var exprNode = describe();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var result = LinqOperationFormatter.BuildCapturedAnyResult(exprNode.Text, preview, count, predicateText);
        var analysis = new AssertionEvaluationResult(context, result);
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertAll<T>(
        IEnumerable<T> source,
        Func<T, bool> predicate,
        string predicateText,
        Func<ExprNode> describe,
        string file,
        int line,
        Func<string?>? messageFactory)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        object? firstFailure = null;
        var passed = Enumerable.All(source, item =>
        {
            var matches = predicate(item);
            if (!matches)
                firstFailure = item;
            return matches;
        });

        var message = messageFactory?.Invoke();
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", "message");

        if (passed)
            return;

        var exprNode = describe();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var result = LinqOperationFormatter.BuildCapturedAllResult(exprNode.Text, firstFailure, predicateText);
        var analysis = new AssertionEvaluationResult(context, result);
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertSequenceEqual<TReceiver, TArgument>(
        TReceiver receiver,
        TArgument argument,
        Func<TReceiver, TArgument, bool> predicate,
        Func<ExprNode> describe,
        string file,
        int line,
        string? message = null)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (predicate(receiver, argument))
            return;

        var exprNode = describe();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var comparison = SequenceEqualComparer.BuildCapturedResult(receiver, argument);
        var result = new BinaryComparisonEvaluationResult(exprNode.Text, ExpressionType.Equal, comparison, false);
        var analysis = new AssertionEvaluationResult(context, result);
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertContains<TReceiver, TArgument>(
        TReceiver receiver,
        TArgument argument,
        Func<TReceiver, TArgument, bool> contains,
        Func<ExprNode> describe,
        string file,
        int line,
        string? message = null)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (contains(receiver, argument))
            return;

        var exprNode = describe();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var result = LinqOperationFormatter.BuildCapturedContainsResult(exprNode.Text, receiver, argument);
        var analysis = new AssertionEvaluationResult(context, result);
        throw new SharpAssertionException(analysis.Format(), analysis);
    }

    public static void AssertComparison<TLeft, TRight>(
        TLeft left,
        TRight right,
        Func<TLeft, TRight, bool> comparison,
        BinaryOp op,
        Func<ExprNode> describe,
        string file,
        int line,
        string? message = null)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (comparison(left, right))
            return;

        var exprNode = describe();
        var context = new AssertionContext(exprNode.Text, file, line, message, exprNode);
        var result = ComparerService.GetComparisonResult(
            new AssertionOperand(left, typeof(TLeft)),
            new AssertionOperand(right, typeof(TRight)));
        var analysis = new AssertionEvaluationResult(context,
            new BinaryComparisonEvaluationResult(exprNode.Text, ToExpressionType(op), result, false));
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

    static ExpressionType ToExpressionType(BinaryOp op) => op switch
    {
        BinaryOp.Eq => ExpressionType.Equal,
        BinaryOp.Ne => ExpressionType.NotEqual,
        BinaryOp.Lt => ExpressionType.LessThan,
        BinaryOp.Le => ExpressionType.LessThanOrEqual,
        BinaryOp.Gt => ExpressionType.GreaterThan,
        BinaryOp.Ge => ExpressionType.GreaterThanOrEqual,
        _ => throw new ArgumentOutOfRangeException(nameof(op))
    };

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

    static Sharp.ExceptionResult<T> RequireCapturedException<T>(
        Sharp.ExceptionResult<T> result, string expr, string file, int line, Func<string?>? messageFactory) where T : Exception
    {
        if (result.HasException)
            return result;

        throw ExceptionCheckFailure(expr, file, line, messageFactory,
            $"Expected exception of type '{typeof(T).FullName}', but no exception was thrown");
    }

    static SharpAssertionException ExceptionCheckFailure(
        string expr, string file, int line, Func<string?>? messageFactory, string details)
    {
        var message = messageFactory?.Invoke();
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", "message");

        var context = new AssertionContext(expr, file, line, message, new ExprNode(expr));
        var analysis = new AssertionEvaluationResult(context,
            new FormattedEvaluationResult(expr, false, [details]));
        return new SharpAssertionException(analysis.Format(), analysis);
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
