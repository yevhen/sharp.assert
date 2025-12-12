using System;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpAssert.Features.Shared;

static class ExpressionValueEvaluator
{
    static readonly MethodInfo SpanToArrayMethod = typeof(ExpressionValueEvaluator).GetMethod(nameof(SpanToArray), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static object? Evaluate(Expression expression) => Compile(expression)();

    public static Func<object> Compile(Expression expression)
    {
        try
        {
            var normalized = Normalize(expression);

            if (TryInterpret(normalized, out var interpreted))
                return Wrap(interpreted);

            if (TryJit(normalized, out var jit))
                return Wrap(jit);

            return () => new EvaluationUnavailable("Evaluation failed");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            return () => new EvaluationUnavailable($"Evaluation failed: {ex.GetType().Name}");
        }
    }

    public static bool TryCompileLambda(LambdaExpression lambda, out Delegate? compiled, out EvaluationUnavailable? failure)
    {
        Exception? interpretedFailure = null;

        try
        {
            compiled = lambda.Compile(preferInterpretation: true);
            failure = null;
            return true;
        }
        catch (Exception ex) when (IsKnownCompilationIssue(ex))
        {
            interpretedFailure = ex;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            compiled = null;
            failure = new EvaluationUnavailable($"Lambda compilation failed: {ex.GetType().Name}");
            return false;
        }

        try
        {
            compiled = lambda.Compile();
            failure = null;
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            compiled = null;
            failure = new EvaluationUnavailable($"Lambda compilation failed: {Unwrap(interpretedFailure ?? ex).GetType().Name}");
            return false;
        }
    }

    static Expression Normalize(Expression expression)
    {
        try
        {
            if (!expression.Type.IsByRefLike)
                return ConvertToObject(expression);

            if (!expression.Type.IsGenericType)
                return Unavailable($"ByRef-like value cannot be evaluated: {expression.Type}");

            var typeDefinition = expression.Type.GetGenericTypeDefinition();
            if (typeDefinition != typeof(Span<>) && typeDefinition != typeof(ReadOnlySpan<>))
                return Unavailable($"ByRef-like value cannot be evaluated: {expression.Type}");

            var elementType = expression.Type.GenericTypeArguments[0];
            var readOnlySpanType = typeof(ReadOnlySpan<>).MakeGenericType(elementType);

            Expression readOnlySpanExpression = typeDefinition == typeof(ReadOnlySpan<>)
                ? expression
                : Expression.Convert(expression, readOnlySpanType);

            var toArray = SpanToArrayMethod.MakeGenericMethod(elementType);
            var call = Expression.Call(toArray, readOnlySpanExpression);
            return ConvertToObject(call);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            return Unavailable($"Normalization failed: {ex.GetType().Name}");
        }

        static ConstantExpression Unavailable(string reason) =>
            Expression.Constant(new EvaluationUnavailable(reason), typeof(object));
    }

    static bool TryInterpret(Expression expression, out Func<object> result)
    {
        try
        {
            result = Expression.Lambda<Func<object>>(expression).Compile(preferInterpretation: true);
            return true;
        }
        catch (Exception ex) when (IsKnownCompilationIssue(ex))
        {
            result = null!;
            return false;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            result = null!;
            return false;
        }
    }

    static bool TryJit(Expression expression, out Func<object> result)
    {
        try
        {
            result = Expression.Lambda<Func<object>>(expression).Compile();
            return true;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            result = null!;
            return false;
        }
    }

    static Func<object> Wrap(Func<object> evaluate) => () =>
    {
        try
        {
            return evaluate();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            return new EvaluationUnavailable($"Evaluation failed: {Unwrap(ex).GetType().Name}");
        }
    };

    static Expression ConvertToObject(Expression expression) =>
        expression.Type == typeof(object) ? expression : Expression.Convert(expression, typeof(object));

    static bool IsKnownCompilationIssue(Exception ex)
    {
        ex = Unwrap(ex);
        return ex is InvalidProgramException or NotSupportedException or TypeLoadException or ArgumentException;
    }

    static Exception Unwrap(Exception ex) => ex is TargetInvocationException { InnerException: not null } tie ? tie.InnerException! : ex;

    static T[] SpanToArray<T>(ReadOnlySpan<T> span) => span.ToArray();
}
