using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace SharpAssert;

internal class ExpressionAnalyzer : ExpressionVisitor
{
    readonly ConcurrentDictionary<Expression, object?> evaluatedValues = new();
    
    public string AnalyzeFailure(Expression<Func<bool>> expression, string originalExpr, string file, int line)
    {
        if (expression.Body is BinaryExpression binaryExpr)
        {
            var leftValue = GetValue(binaryExpr.Left);
            var rightValue = GetValue(binaryExpr.Right);
            var result = EvaluateBinaryOperation(binaryExpr.NodeType, leftValue, rightValue);
            
            if (result)
                return string.Empty;
            
            return AnalyzeBinaryFailure(binaryExpr, leftValue, rightValue, originalExpr, file, line);
        }

        var expressionResult = GetValue(expression.Body);
        if (expressionResult is true)
            return string.Empty;
            
        return AssertionFormatter.FormatAssertionFailure(originalExpr, file, line);
    }

    string AnalyzeBinaryFailure(BinaryExpression binaryExpr, object? leftValue, object? rightValue, string originalExpr, string file, int line)
    {
        var operatorSymbol = GetOperatorSymbol(binaryExpr.NodeType);
        var leftDisplay = FormatValue(leftValue);
        var rightDisplay = FormatValue(rightValue);
        
        var locationPart = AssertionFormatter.FormatLocation(file, line);
        
        return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
               $"  Left:  {leftDisplay}\n" +
               $"  Right: {rightDisplay}";
    }

    bool EvaluateBinaryOperation(ExpressionType nodeType, object? left, object? right)
    {
        try
        {
            return nodeType switch
            {
                ExpressionType.Equal => Equals(left, right),
                ExpressionType.NotEqual => !Equals(left, right),
                ExpressionType.LessThan => Comparer<object>.Default.Compare(left, right) < 0,
                ExpressionType.LessThanOrEqual => Comparer<object>.Default.Compare(left, right) <= 0,
                ExpressionType.GreaterThan => Comparer<object>.Default.Compare(left, right) > 0,
                ExpressionType.GreaterThanOrEqual => Comparer<object>.Default.Compare(left, right) >= 0,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    string GetOperatorSymbol(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            _ => nodeType.ToString()
        };
    }

    string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            _ => value.ToString() ?? "null"
        };
    }

    object? GetValue(Expression expression)
    {
        if (evaluatedValues.TryGetValue(expression, out var cachedValue))
            return cachedValue;
            
        var result = CompileAndEvaluate(expression);
        evaluatedValues[expression] = result;
        
        return result;
    }
    
    static object? CompileAndEvaluate(Expression expression)
    {
        var compiled = Expression.Lambda(expression).Compile();
        return compiled.DynamicInvoke();
    }
}