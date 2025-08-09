using System.Linq.Expressions;

namespace SharpAssert;

internal class ExpressionAnalyzer : ExpressionVisitor
{
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
                return AnalyzeBinaryFailure(leftValue, rightValue, originalExpr, file, line);
            }
            case UnaryExpression { NodeType: ExpressionType.Not } unaryExpr:
            {
                return AnalyzeNotFailure(unaryExpr, originalExpr, file, line);
            }
        }

        var expressionResult = GetValue(expression.Body);
        return expressionResult is true
            ? string.Empty :
            AssertionFormatter.FormatAssertionFailure(originalExpr, file, line);
    }

    string AnalyzeLogicalBinaryFailure(BinaryExpression binaryExpr, string originalExpr, string file, int line)
    {
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

    static string AnalyzeBinaryFailure(object? leftValue, object? rightValue, string originalExpr, string file, int line)
    {
        var leftDisplay = FormatValue(leftValue);
        var rightDisplay = FormatValue(rightValue);
        
        var locationPart = AssertionFormatter.FormatLocation(file, line);
        
        return $"Assertion failed: {originalExpr}  at {locationPart}\n" +
               $"  Left:  {leftDisplay}\n" +
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

    object? GetValue(Expression expression) => CompileAndEvaluate(expression);
    
    static object? CompileAndEvaluate(Expression expression)
    {
        var compiled = Expression.Lambda(expression).Compile();
        return compiled.DynamicInvoke();
    }
}