using System.Linq.Expressions;
using System.Text;

namespace SharpAssert;

class ReadableExpressionFormatter : ExpressionVisitor
{
    readonly StringBuilder sb = new();
    bool isInsideLogicalExpression;

    public static string Format(Expression expression)
    {
        var formatter = new ReadableExpressionFormatter();
        formatter.Visit(expression);
        return formatter.sb.ToString();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var needsParens = isInsideLogicalExpression && IsLogicalOperator(node.NodeType);

        if (needsParens)
            sb.Append('(');

        var wasInsideLogical = isInsideLogicalExpression;
        if (IsLogicalOperator(node.NodeType))
            isInsideLogicalExpression = true;

        Visit(node.Left);
        sb.Append(' ');
        sb.Append(GetOperatorSymbol(node.NodeType));
        sb.Append(' ');
        Visit(node.Right);

        isInsideLogicalExpression = wasInsideLogical;

        if (needsParens)
            sb.Append(')');

        return node;
    }

    static bool IsLogicalOperator(ExpressionType nodeType) =>
        nodeType is ExpressionType.AndAlso or ExpressionType.OrElse;

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null)
        {
            if (node.Expression is ConstantExpression { Value: not null } constant)
            {
                if (constant.Type.Name.Contains("DisplayClass"))
                {
                    sb.Append(node.Member.Name);
                    return node;
                }
            }

            Visit(node.Expression);
            sb.Append('.');
        }

        sb.Append(node.Member.Name);
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Type.Name.Contains("DisplayClass"))
            return node;

        if (node.Value == null)
            sb.Append("null");
        else if (node.Type == typeof(string))
            sb.Append($"\"{node.Value}\"");
        else
            sb.Append(node.Value);

        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object != null)
        {
            Visit(node.Object);
            sb.Append('.');
        }

        sb.Append(node.Method.Name);
        sb.Append('(');

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            Visit(node.Arguments[i]);
        }

        sb.Append(')');
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node.NodeType)
        {
            case ExpressionType.Not:
                sb.Append('!');
                Visit(node.Operand);
                break;
            case ExpressionType.Negate:
                sb.Append('-');
                Visit(node.Operand);
                break;
            case ExpressionType.Convert:
            case ExpressionType.ConvertChecked:
                Visit(node.Operand);
                break;
            default:
                sb.Append(node.NodeType);
                sb.Append('(');
                Visit(node.Operand);
                sb.Append(')');
                break;
        }

        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        sb.Append(node.Name ?? "param");
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node.Parameters.Count == 1)
        {
            sb.Append(node.Parameters[0].Name ?? "x");
        }
        else
        {
            sb.Append('(');
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(node.Parameters[i].Name ?? $"param{i}");
            }
            sb.Append(')');
        }

        sb.Append(" => ");
        Visit(node.Body);
        return node;
    }

    static string GetOperatorSymbol(ExpressionType nodeType) => nodeType switch
    {
        ExpressionType.Add => "+",
        ExpressionType.AddChecked => "+",
        ExpressionType.Subtract => "-",
        ExpressionType.SubtractChecked => "-",
        ExpressionType.Multiply => "*",
        ExpressionType.MultiplyChecked => "*",
        ExpressionType.Divide => "/",
        ExpressionType.Modulo => "%",
        ExpressionType.And => "&",
        ExpressionType.AndAlso => "&&",
        ExpressionType.Or => "|",
        ExpressionType.OrElse => "||",
        ExpressionType.Equal => "==",
        ExpressionType.NotEqual => "!=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.ExclusiveOr => "^",
        ExpressionType.LeftShift => "<<",
        ExpressionType.RightShift => ">>",
        _ => nodeType.ToString()
    };
}
