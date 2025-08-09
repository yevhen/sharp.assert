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
            if (binaryExpr.NodeType == ExpressionType.AndAlso || binaryExpr.NodeType == ExpressionType.OrElse)
            {
                var logicalResult = GetValue(binaryExpr);
                if (logicalResult is true)
                    return string.Empty;
                    
                return AnalyzeLogicalBinaryFailure(binaryExpr, originalExpr, file, line);
            }
            
            var leftValue = GetValue(binaryExpr.Left);
            var rightValue = GetValue(binaryExpr.Right);
            var result = EvaluateBinaryOperation(binaryExpr.NodeType, leftValue, rightValue);
            
            if (result)
                return string.Empty;
            
            return AnalyzeBinaryFailure(binaryExpr, leftValue, rightValue, originalExpr, file, line);
        }
        
        if (expression.Body is UnaryExpression unaryExpr && unaryExpr.NodeType == ExpressionType.Not)
        {
            var notResult = GetValue(unaryExpr);
            if (notResult is true)
                return string.Empty;
                
            return AnalyzeNotFailure(unaryExpr, originalExpr, file, line);
        }

        var expressionResult = GetValue(expression.Body);
        if (expressionResult is true)
            return string.Empty;
            
        return AssertionFormatter.FormatAssertionFailure(originalExpr, file, line);
    }

    string AnalyzeLogicalBinaryFailure(BinaryExpression binaryExpr, string originalExpr, string file, int line)
    {
        var locationPart = AssertionFormatter.FormatLocation(file, line);
        var operatorSymbol = GetOperatorSymbol(binaryExpr.NodeType);
        
        var leftValue = GetValue(binaryExpr.Left);
        var leftBool = (bool)leftValue!;
        
        if (binaryExpr.NodeType == ExpressionType.AndAlso)
        {
            if (!leftBool)
            {
                return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
                       $"  Left:  {FormatValue(leftValue)} (short-circuit)\n" +
                       $"  &&: Left operand was false";
            }
            else
            {
                var rightValue = GetValue(binaryExpr.Right);
                return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
                       $"  Left:  {FormatValue(leftValue)}\n" +
                       $"  Right: {FormatValue(rightValue)}\n" +
                       $"  &&: Right operand was false";
            }
        }
        else // OrElse
        {
            if (leftBool)
            {
                return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
                       $"  Left:  {FormatValue(leftValue)}\n" +
                       $"  ||: Left operand was true (this should not fail)";
            }
            else
            {
                var rightValue = GetValue(binaryExpr.Right);
                return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
                       $"  Left:  {FormatValue(leftValue)}\n" +
                       $"  Right: {FormatValue(rightValue)}\n" +
                       $"  ||: Both operands were false";
            }
        }
    }

    string AnalyzeNotFailure(UnaryExpression unaryExpr, string originalExpr, string file, int line)
    {
        var operandValue = GetValue(unaryExpr.Operand);
        var locationPart = AssertionFormatter.FormatLocation(file, line);
        
        return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
               $"  Operand: {FormatValue(operandValue)}\n" +
               $"  !: Operand was {FormatValue(operandValue)}";
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
            ExpressionType.AndAlso => "&&",
            ExpressionType.OrElse => "||",
            ExpressionType.Not => "!",
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