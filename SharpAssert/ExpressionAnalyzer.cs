using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace SharpAssert;

/// <summary>Analyzes expression trees and provides detailed failure messages.</summary>
internal class ExpressionAnalyzer : ExpressionVisitor
{
    private readonly ConcurrentDictionary<Expression, object?> _evaluatedValues = new();
    
    /// <summary>
    /// Analyzes the expression and returns a detailed failure message if it evaluates to false.
    /// </summary>
    public string AnalyzeFailure(Expression<Func<bool>> expression, string originalExpr, string file, int line)
    {
        // If it's a binary expression, we can analyze it directly without pre-evaluation
        if (expression.Body is BinaryExpression binaryExpr)
        {
            // Evaluate operands first
            var leftValue = GetValue(binaryExpr.Left);
            var rightValue = GetValue(binaryExpr.Right);
            
            // Then check if the binary operation result would be true
            var result = EvaluateBinaryOperation(binaryExpr.NodeType, leftValue, rightValue);
            
            if (result)
            {
                return string.Empty; // No failure
            }
            
            return AnalyzeBinaryFailure(binaryExpr, leftValue, rightValue, originalExpr, file, line);
        }

        // For non-binary expressions, evaluate the entire expression
        var expressionResult = GetValue(expression.Body);
        
        if (expressionResult is true)
        {
            return string.Empty; // No failure
        }

        // Default failure message
        return FormatAssertionFailure(originalExpr, file, line);
    }

    private string AnalyzeBinaryFailure(BinaryExpression binaryExpr, object? leftValue, object? rightValue, string originalExpr, string file, int line)
    {
        var operatorSymbol = GetOperatorSymbol(binaryExpr.NodeType);
        var leftDisplay = FormatValue(leftValue);
        var rightDisplay = FormatValue(rightValue);
        
        var locationPart = string.IsNullOrEmpty(file) ? $"line {line}" : $"{file}:{line}";
        
        return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
               $"  Left:  {leftDisplay}\n" +
               $"  Right: {rightDisplay}";
    }

    private bool EvaluateBinaryOperation(ExpressionType nodeType, object? left, object? right)
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
            // If we can't compare, assume the operation failed (which is why we're analyzing)
            return false;
        }
    }

    private string GetOperatorSymbol(ExpressionType nodeType)
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

    private string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            char c => $"'{c}'",
            _ => value.ToString() ?? "null"
        };
    }

    private object? GetValue(Expression expression)
    {
        // Check cache first to ensure single evaluation
        if (_evaluatedValues.TryGetValue(expression, out var cachedValue))
        {
            return cachedValue;
        }

        // Evaluate and cache
        var compiled = Expression.Lambda(expression).Compile();
        var result = compiled.DynamicInvoke();
        _evaluatedValues[expression] = result;
        
        return result;
    }

    private static string FormatAssertionFailure(string expr, string file, int line)
    {
        var expressionPart = string.IsNullOrEmpty(expr) ? "false" : expr;
        var locationPart = string.IsNullOrEmpty(file) ? $"line {line}" : $"{file}:{line}";
        return $"Assertion failed: {expressionPart}  at {locationPart}";
    }
}