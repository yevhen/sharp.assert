using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpAssert.Features.Shared;

static class ExpressionValueEvaluator
{
    static readonly MethodInfo SpanToArrayMethod = typeof(ExpressionValueEvaluator).GetMethod(nameof(SpanToArray), BindingFlags.NonPublic | BindingFlags.Static)!;
    static readonly Func<object> NullEvaluator = () => null!;

    public static object? Evaluate(Expression expression) => Compile(expression)();

    public static Func<object> Compile(Expression expression)
    {
        var normalized = Normalize(expression);

        if (TryInterpret(normalized, out var interpreted))
            return interpreted;

        if (TryJit(normalized, out var jit))
            return jit;

        if (TryManualEvaluate(normalized, out var manual))
            return manual;

        return () => new EvaluationUnavailable("Evaluation failed");
    }

    static Expression Normalize(Expression expression)
    {
        if (!expression.Type.IsByRefLike)
            return Expression.Convert(expression, typeof(object));

        if (!expression.Type.IsGenericType)
            return Expression.Constant(null, typeof(object));

        var elementType = expression.Type.GenericTypeArguments[0];
        var readOnlySpanType = typeof(ReadOnlySpan<>).MakeGenericType(elementType);

        var spanExpression = expression.Type == readOnlySpanType
            ? expression
            : Expression.Convert(expression, readOnlySpanType);

        var toArray = SpanToArrayMethod.MakeGenericMethod(elementType);
        var call = Expression.Call(toArray, spanExpression);
        return Expression.Convert(call, typeof(object));
    }

    static bool TryInterpret(Expression expression, out Func<object> result)
    {
        try
        {
            result = Expression.Lambda<Func<object>>(expression).Compile(preferInterpretation: true);
            return true;
        }
        catch
        {
            result = NullEvaluator;
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
        catch
        {
            result = NullEvaluator;
            return false;
        }
    }

    static bool TryManualEvaluate(Expression expression, out Func<object> result)
    {
        if (TryEvaluateExpression(expression, out var value))
        {
            result = () => value;
            return true;
        }

        result = NullEvaluator;
        return false;
    }

    static bool TryEvaluateExpression(Expression expression, out object value)
    {
        switch (expression)
        {
            case ConstantExpression constant:
                value = constant.Value!;
                return true;
            case UnaryExpression { NodeType: ExpressionType.Convert } unary when TryEvaluateExpression(unary.Operand, out var operand):
                value = operand;
                return true;
            case MemberExpression member when TryEvaluateMember(member, out var memberValue):
                value = memberValue;
                return true;
            case MethodCallExpression call when TryEvaluateMethodCall(call, out var callResult):
                value = callResult;
                return true;
            default:
                value = new EvaluationUnavailable($"Unsupported expression: {expression.NodeType}");
                return false;
        }
    }

    static bool TryEvaluateMethodCall(MethodCallExpression call, out object result)
    {
        if (!TryEvaluateArguments(call, out var args))
        {
            result = new EvaluationUnavailable("Argument evaluation failed");
            return false;
        }

        try
        {
            object? obj = null;
            var target = call.Object != null && TryEvaluateExpression(call.Object, out obj) ? obj : null;
            result = call.Method.Invoke(target, args)!;
            return true;
        }
        catch
        {
            result = new EvaluationUnavailable("Method invocation failed");
            return false;
        }
    }

    static bool TryEvaluateArguments(MethodCallExpression call, out object?[] args)
    {
        args = new object?[call.Arguments.Count];

        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (!TryEvaluateExpression(call.Arguments[i], out var arg))
                return false;

            args[i] = arg;
        }

        return true;
    }

    static bool TryEvaluateMember(MemberExpression member, out object value)
    {
        object? target = null;
        var hasTarget = member.Expression != null && TryEvaluateExpression(member.Expression, out target);

        if (member.Member is FieldInfo field)
        {
            value = field.GetValue(hasTarget ? TargetOrNull(target) : null)!;
            return true;
        }

        if (member.Member is PropertyInfo prop)
        {
            value = prop.GetValue(hasTarget ? TargetOrNull(target) : null)!;
            return true;
        }

        value = new EvaluationUnavailable($"Unsupported member: {member.Member.Name}");
        return false;

        static object? TargetOrNull(object? targetValue) => targetValue is EvaluationUnavailable ? null : targetValue;
    }

    static T[] SpanToArray<T>(ReadOnlySpan<T> span) => span.ToArray();
}
