using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace SharpAssert;

internal class ExpressionAnalyzer : ExpressionVisitor
{
    readonly ConcurrentDictionary<Expression, object?> evaluatedValues = new();
    
    public string AnalyzeFailure(Expression<Func<bool>> expression, string originalExpr, string file, int line)
    {
        switch (expression.Body)
        {
            case BinaryExpression binaryExpr:
            {
                if (binaryExpr.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
                {
                    var logicalResult = GetValue(binaryExpr);
                    return logicalResult is true ?
                        string.Empty :
                        AnalyzeLogicalBinaryFailure(binaryExpr, originalExpr, file, line);
                }

                var leftValue = GetValue(binaryExpr.Left);
                var rightValue = GetValue(binaryExpr.Right);
                var result = EvaluateBinaryOperation(binaryExpr.NodeType, leftValue, rightValue);

                return result ?
                    string.Empty :
                    AnalyzeBinaryFailure(binaryExpr, leftValue, rightValue, originalExpr, file, line);
            }
            case UnaryExpression { NodeType: ExpressionType.Not } unaryExpr:
            {
                var notResult = GetValue(unaryExpr);
                return notResult is true
                    ? string.Empty :
                    AnalyzeNotFailure(unaryExpr, originalExpr, file, line);
            }
        }

        var expressionResult = GetValue(expression.Body);
        return expressionResult is true
            ? string.Empty :
            AssertionFormatter.FormatAssertionFailure(originalExpr, file, line);
    }

    string AnalyzeLogicalBinaryFailure(BinaryExpression binaryExpr, string originalExpr, string file, int line)
    {
        var operatorSymbol = GetOperatorSymbol(binaryExpr.NodeType);
        var locationPart = AssertionFormatter.FormatLocation(file, line);
        
        var leftValue = GetValue(binaryExpr.Left);
        var leftBool = (bool)leftValue!;
        
        if (binaryExpr.NodeType == ExpressionType.AndAlso)
        {
            if (!leftBool)
                return FormatLogicalFailure(originalExpr, locationPart, leftValue, null, "&&: Left operand was false", true);
            
            var rightValue = GetValue(binaryExpr.Right);
            return FormatLogicalFailure(originalExpr, locationPart, leftValue, rightValue, "&&: Right operand was false", false);
        }
        
        // OrElse
        if (leftBool)
            return FormatLogicalFailure(originalExpr, locationPart, leftValue, null, "||: Left operand was true (this should not fail)", false);
        
        var rightValueOrElse = GetValue(binaryExpr.Right);
        return FormatLogicalFailure(originalExpr, locationPart, leftValue, rightValueOrElse, "||: Both operands were false", false);
    }

    static string FormatLogicalFailure(string originalExpr, string locationPart, object? leftValue, object? rightValue, string explanation, bool isShortCircuit)
    {
        var result = $"Assertion failed: {originalExpr}  at {locationPart}\n" +
                     $"  Left:  {FormatValue(leftValue)}{(isShortCircuit ? " (short-circuit)" : "")}";
        
        if (rightValue != null)
            result += $"\n  Right: {FormatValue(rightValue)}";
            
        result += $"\n  {explanation}";
        return result;
    }

    string AnalyzeNotFailure(UnaryExpression unaryExpr, string originalExpr, string file, int line)
    {
        var operandValue = GetValue(unaryExpr.Operand);
        var locationPart = AssertionFormatter.FormatLocation(file, line);
        
        return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
               $"  Operand: {FormatValue(operandValue)}\n" +
               $"  !: Operand was {FormatValue(operandValue)}";
    }

    static string AnalyzeBinaryFailure(BinaryExpression binaryExpr, object? leftValue, object? rightValue, string originalExpr, string file, int line)
    {
        var operatorSymbol = GetOperatorSymbol(binaryExpr.NodeType);
        var leftDisplay = FormatValue(leftValue);
        var rightDisplay = FormatValue(rightValue);
        
        var locationPart = AssertionFormatter.FormatLocation(file, line);
        
        return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
               $"  Left:  {leftDisplay}\n" +
               $"  Right: {rightDisplay}";
    }

    static bool EvaluateBinaryOperation(ExpressionType nodeType, object? left, object? right)
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

    static string GetOperatorSymbol(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "==",
            ExpressionType.NotEqual => "!=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.AndAlso => "&&",
            ExpressionType.OrElse => "||",
            ExpressionType.Not => "!",
            _ => nodeType.ToString()
        };
    }

    static string FormatValue(object? value)
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