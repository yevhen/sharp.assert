using System.Collections;
using System.Linq.Expressions;
using static System.Linq.Expressions.ExpressionType;

namespace SharpAssert;

class ExpressionAnalyzer : ExpressionVisitor
{
    static readonly IComparisonFormatter DefaultFormatter = new DefaultComparisonFormatter();

    static readonly IComparisonFormatter[] ComparisonFormatters =
    [
        new StringComparisonFormatter(),
        new CollectionComparisonFormatter(),
        new ObjectComparisonFormatter(),
    ];

    static readonly string[] LinqOperationMethods = ["Contains", "Any", "All"];
    const string SequenceEqualMethod = "SequenceEqual";

    public string AnalyzeFailure(Expression<Func<bool>> expression, AssertionContext context)
    {
        switch (expression.Body)
        {
            case BinaryExpression binaryExpr:
            {
                if (binaryExpr.NodeType is AndAlso or OrElse)
                    return FormatIfFailed(binaryExpr, () => AnalyzeLogicalBinaryFailure(binaryExpr, context));

                var leftValue = GetValue(binaryExpr.Left);
                var rightValue = GetValue(binaryExpr.Right);

                return EvaluateBinaryExpression(binaryExpr.NodeType, leftValue, rightValue)
                    ? string.Empty
                    : AnalyzeBinaryFailure(leftValue, rightValue, context);
            }
            case UnaryExpression { NodeType: Not } unaryExpr:
            {
                return FormatIfFailed(unaryExpr, () => AnalyzeNotFailure(unaryExpr, context));
            }
            case MethodCallExpression methodCall:
            {
                var methodName = methodCall.Method.Name;

                if (LinqOperationMethods.Contains(methodName))
                    return FormatIfFailed(methodCall, () => LinqOperationFormatter.FormatLinqOperation(methodCall, context));

                if (methodName == SequenceEqualMethod)
                    return FormatIfFailed(methodCall, () => SequenceEqualFormatter.FormatSequenceEqual(methodCall, context));

                break;
            }
        }

        return FormatIfFailed(expression.Body, () => AssertionFormatter.FormatAssertionFailure(context));
    }

    static string AnalyzeLogicalBinaryFailure(BinaryExpression binaryExpr, AssertionContext context)
    {
        var locationPart = GetLocationPart(context);
        
        var leftValue = GetValue(binaryExpr.Left);
        var leftBool = (bool)leftValue!;
        
        if (binaryExpr.NodeType != AndAlso)
        {
            var rightValueOrElse = GetValue(binaryExpr.Right);
            return FormatLogicalFailure(context, locationPart, leftValue, rightValueOrElse, "||: Both operands were false", false);
        }

        if (!leftBool)
            return FormatLogicalFailure(context, locationPart, leftValue, null, "&&: Left operand was false", true);

        var rightValue = GetValue(binaryExpr.Right);
        return FormatLogicalFailure(context, locationPart, leftValue, rightValue, "&&: Right operand was false", false);
    }

    static string GetLocationPart(AssertionContext context) => 
        AssertionFormatter.FormatLocation(context.File, context.Line);

    static string FormatIfFailed(Expression expression, Func<string> formatter) =>
        GetValue(expression) is true ? string.Empty : formatter();

    static string FormatBaseMessage(AssertionContext context, string locationPart) =>
        context.Message is not null
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";

    static string FormatLogicalFailure(AssertionContext context, string locationPart, object? leftValue, object? rightValue, string explanation, bool isShortCircuit)
    {
        var baseMessage = FormatBaseMessage(context, locationPart);
            
        var result = baseMessage + $"  Left:  {FormatValue(leftValue)}{(isShortCircuit ? " (short-circuit)" : "")}";
        
        if (rightValue is not null)
            result += $"\n  Right: {FormatValue(rightValue)}";
            
        result += $"\n  {explanation}";

        return result;
    }

    static string AnalyzeNotFailure(UnaryExpression unaryExpr, AssertionContext context)
    {
        var operandValue = GetValue(unaryExpr.Operand);
        var locationPart = GetLocationPart(context);
        
        var baseMessage = FormatBaseMessage(context, locationPart);
        
        return baseMessage + $"  Operand: {FormatValue(operandValue)}\n" +
               $"  !: Operand was {FormatValue(operandValue)}";
    }

    static string AnalyzeBinaryFailure(object? leftValue, object? rightValue, AssertionContext context)
    {
        var locationPart = GetLocationPart(context);
        
        var baseMessage = FormatBaseMessage(context, locationPart);
        
        var formatter = GetComparisonFormatter(leftValue, rightValue);
        return baseMessage + formatter.FormatComparison(leftValue, rightValue);
    }

    static IComparisonFormatter GetComparisonFormatter(object? leftValue, object? rightValue)
    {
        foreach (var formatter in ComparisonFormatters)
        {
            if (formatter.CanFormat(leftValue, rightValue))
                return formatter;
        }
        
        return DefaultFormatter;
    }

    static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => value.ToString()!
    };

    static object? GetValue(Expression expression) => CompileAndEvaluate(expression);
    
    static object? CompileAndEvaluate(Expression expression)
    {
        var compiled = Expression.Lambda(expression).Compile();
        return compiled.DynamicInvoke();
    }
    
    static bool EvaluateBinaryExpression(ExpressionType nodeType, object? leftValue, object? rightValue) => nodeType switch
    {
        Equal => Equals(leftValue, rightValue),
        NotEqual => !Equals(leftValue, rightValue),
        LessThan => Comparer.Default.Compare(leftValue, rightValue) < 0,
        LessThanOrEqual => Comparer.Default.Compare(leftValue, rightValue) <= 0,
        GreaterThan => Comparer.Default.Compare(leftValue, rightValue) > 0,
        GreaterThanOrEqual => Comparer.Default.Compare(leftValue, rightValue) >= 0,
        _ => false
    };
}