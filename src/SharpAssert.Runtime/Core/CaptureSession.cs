#pragma warning disable CS1591
using System.Linq.Expressions;
using System.Reflection;

namespace SharpAssert.Core;

public sealed class CaptureSession(int count)
{
    readonly object?[] values = new object?[count];

    public T Record<T>(int index, T value)
    {
        values[index] = value;
        return value;
    }

    internal object? Read(int index) => values[index];
}

sealed class CapturedExpressionVisitor(CaptureSession session, Dictionary<Expression, object?> cache) : ExpressionVisitor
{
    static readonly MethodInfo RecordMethod = typeof(CaptureSession).GetMethod(nameof(CaptureSession.Record))!;

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node is { NodeType: ExpressionType.And, Type: var type, Method: null } && type == typeof(bool))
            return Expression.AndAlso(Visit(node.Left), Visit(node.Right));

        return base.VisitBinary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (!node.Method.IsGenericMethod || node.Method.GetGenericMethodDefinition() != RecordMethod)
            return base.VisitMethodCall(node);

        var index = (int)((ConstantExpression)node.Arguments[0]).Value!;
        var expression = Visit(node.Arguments[1]);
        cache[expression] = session.Read(index);
        return expression;
    }
}
