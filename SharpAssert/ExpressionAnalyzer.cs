using System.Collections;
using System.Linq.Expressions;
using static System.Linq.Expressions.ExpressionType;

namespace SharpAssert;

internal class ExpressionAnalyzer : ExpressionVisitor
{
    public string AnalyzeFailure(Expression<Func<bool>> expression, AssertionContext context)
    {
        switch (expression.Body)
        {
            case BinaryExpression binaryExpr:
            {
                if (binaryExpr.NodeType is AndAlso or OrElse)
                {
                    return GetValue(binaryExpr) is true
                        ? string.Empty
                        : AnalyzeLogicalBinaryFailure(binaryExpr, context);
                }

                var leftValue = GetValue(binaryExpr.Left);
                var rightValue = GetValue(binaryExpr.Right);

                return EvaluateBinaryExpression(binaryExpr.NodeType, leftValue, rightValue)
                    ? string.Empty
                    : AnalyzeBinaryFailure(leftValue, rightValue, context);
            }
            case UnaryExpression { NodeType: Not } unaryExpr:
            {
                return GetValue(unaryExpr) is true
                    ? string.Empty
                    : AnalyzeNotFailure(unaryExpr, context);
            }
        }

        var expressionResult = GetValue(expression.Body);
        return expressionResult is true
            ? string.Empty :
            AssertionFormatter.FormatAssertionFailure(context);
    }

    string AnalyzeLogicalBinaryFailure(BinaryExpression binaryExpr, AssertionContext context)
    {
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
        
        var leftValue = GetValue(binaryExpr.Left);
        var leftBool = (bool)leftValue!;
        
        if (binaryExpr.NodeType == AndAlso)
        {
            if (!leftBool)
                return FormatLogicalFailure(context, locationPart, leftValue, null, "&&: Left operand was false", true);
            
            var rightValue = GetValue(binaryExpr.Right);
            return FormatLogicalFailure(context, locationPart, leftValue, rightValue, "&&: Right operand was false", false);
        }
        
        var rightValueOrElse = GetValue(binaryExpr.Right);

        return FormatLogicalFailure(context, locationPart, leftValue, rightValueOrElse, "||: Both operands were false", false);
    }

    static string FormatLogicalFailure(AssertionContext context, string locationPart, object? leftValue, object? rightValue, string explanation, bool isShortCircuit)
    {
        var baseMessage = context.Message is not null 
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";
            
        var result = baseMessage + $"  Left:  {FormatValue(leftValue)}{(isShortCircuit ? " (short-circuit)" : "")}";
        
        if (rightValue != null)
            result += $"\n  Right: {FormatValue(rightValue)}";
            
        result += $"\n  {explanation}";
        return result;
    }

    string AnalyzeNotFailure(UnaryExpression unaryExpr, AssertionContext context)
    {
        var operandValue = GetValue(unaryExpr.Operand);
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
        
        var baseMessage = context.Message is not null 
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";
        
        return baseMessage + $"  Operand: {FormatValue(operandValue)}\n" +
               $"  !: Operand was {FormatValue(operandValue)}";
    }

    static string AnalyzeBinaryFailure(object? leftValue, object? rightValue, AssertionContext context)
    {
        var leftDisplay = FormatValue(leftValue);
        var rightDisplay = FormatValue(rightValue);
        
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
        
        var baseMessage = context.Message is not null 
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";
        
        return baseMessage + $"  Left:  {leftDisplay}\n" +
               $"  Right: {rightDisplay}";
    }


    static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => value.ToString()!
        };
    }

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