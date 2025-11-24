using System.Linq.Expressions;
using static System.Linq.Expressions.ExpressionType;

namespace SharpAssert;

static class ExpressionDisplay
{
    public static string GetIdentifierOrPath(Expression expr) =>
        expr switch
        {
            MemberExpression member => GetMemberPath(member),
            ParameterExpression p => p.Name ?? "param",
            ConstantExpression c => c.Value is null ? "null" : ValueFormatter.Format(c.Value),
            _ => expr.ToString()
        };

    public static string FormatBinary(string left, string op, string right, bool needsParens) =>
        needsParens ? $"({left} {op} {right})" : $"{left} {op} {right}";

    public static string FormatUnary(string op, string operand, bool needsParens) =>
        needsParens ? $"{op}({operand})" : $"{op}{operand}";

    public static string OperatorSymbol(ExpressionType nodeType) => nodeType switch
    {
        Add or AddChecked => "+",
        Subtract or SubtractChecked => "-",
        Multiply or MultiplyChecked => "*",
        Divide => "/",
        Modulo => "%",
        And => "&",
        AndAlso => "&&",
        Or => "|",
        OrElse => "||",
        Equal => "==",
        NotEqual => "!=",
        LessThan => "<",
        LessThanOrEqual => "<=",
        GreaterThan => ">",
        GreaterThanOrEqual => ">=",
        ExclusiveOr => "^",
        LeftShift => "<<",
        RightShift => ">>",
        _ => nodeType.ToString()
    };

    public static bool IsLogical(ExpressionType nodeType) =>
        nodeType is AndAlso or OrElse;

    static string GetMemberPath(MemberExpression member)
    {
        var parts = new Stack<string>();
        Expression? current = member;

        while (current is MemberExpression m)
        {
            parts.Push(m.Member.Name);
            current = m.Expression;
        }

        if (current is ParameterExpression p)
            parts.Push(p.Name ?? "param");

        return string.Join(".", parts.Where(x => !string.IsNullOrEmpty(x)));
    }
}
