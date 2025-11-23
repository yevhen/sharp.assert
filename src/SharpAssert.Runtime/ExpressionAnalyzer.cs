using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using static System.Linq.Expressions.ExpressionType;

namespace SharpAssert;

abstract class ExpressionAnalyzer : ExpressionVisitor
{
    static readonly string[] LinqOperationMethods = ["Contains", "Any", "All"];
    const string SequenceEqualMethod = "SequenceEqual";
    static readonly ReferenceEqualityComparer ExprComparer = ReferenceEqualityComparer.Instance;

    public static string AnalyzeFailure(Expression<Func<bool>> expression, AssertionContext context)
    {
        var analysis = Analyze(expression, context);
        if (analysis.Passed)
            return string.Empty;

        var formatter = new StringEvaluationFormatter();
        return formatter.Format(analysis);
    }

    internal static AssertionEvaluationResult Analyze(Expression<Func<bool>> expression, AssertionContext context)
    {
        var cache = new Dictionary<Expression, object?>(ExprComparer);
        var result = AnalyzeExpression(expression.Body, cache);
        return new AssertionEvaluationResult(context, result);
    }

    static EvaluationResult AnalyzeExpression(Expression expression, Dictionary<Expression, object?> cache)
    {
        switch (expression)
        {
            case BinaryExpression binaryExpr:
                return AnalyzeBinaryExpression(binaryExpr, cache);
            case UnaryExpression { NodeType: Not } unaryExpr:
                return AnalyzeNot(unaryExpr, cache);
            case MethodCallExpression methodCall:
                return AnalyzeMethodCall(methodCall, cache);
        }

        var exprText = ReadableExpressionFormatter.Format(expression);
        var value = GetValue(expression, cache);

        return new ValueEvaluationResult(exprText, value, expression.Type);
    }

    static EvaluationResult AnalyzeBinaryExpression(BinaryExpression binaryExpr, Dictionary<Expression, object?> cache)
    {
        if (binaryExpr.NodeType is AndAlso or OrElse)
            return AnalyzeLogicalBinary(binaryExpr, cache);

        var leftValue = GetValue(binaryExpr.Left, cache);
        var rightValue = GetValue(binaryExpr.Right, cache);
        var exprText = ReadableExpressionFormatter.Format(binaryExpr);

        var leftOperand = new AssertionOperand(leftValue, binaryExpr.Left.Type);
        var rightOperand = new AssertionOperand(rightValue, binaryExpr.Right.Type);
        var comparison = ComparisonFormatterService.GetComparisonResult(leftOperand, rightOperand);

        var resultValue = EvaluateBinaryExpression(binaryExpr.NodeType, leftValue, rightValue);

        return new BinaryComparisonEvaluationResult(exprText, binaryExpr.NodeType, comparison, resultValue);
    }

    static EvaluationResult AnalyzeLogicalBinary(BinaryExpression binaryExpr, Dictionary<Expression, object?> cache)
    {
        var exprText = ReadableExpressionFormatter.Format(binaryExpr);
        var leftValue = GetValue(binaryExpr.Left, cache);
        var leftBool = (bool)leftValue!;

        if (binaryExpr.NodeType == OrElse)
        {
            var leftAnalysis = AnalyzeExpression(binaryExpr.Left, cache);
            var rightAnalysis = AnalyzeExpression(binaryExpr.Right, cache);
            var orValue = leftBool || (bool)GetValue(binaryExpr.Right, cache)!;

            return new LogicalEvaluationResult(exprText, LogicalOperator.OrElse, leftAnalysis, rightAnalysis, orValue, false);
        }

        // AND
        if (!leftBool)
        {
            var leftAnalysis = AnalyzeExpression(binaryExpr.Left, cache);
            return new LogicalEvaluationResult(exprText, LogicalOperator.AndAlso, leftAnalysis, null, false, true);
        }

        var leftResult = AnalyzeExpression(binaryExpr.Left, cache);
        var rightResult = AnalyzeExpression(binaryExpr.Right, cache);
        var rightBool = (bool)GetValue(binaryExpr.Right, cache)!;
        var andValue = leftBool && rightBool;

        return new LogicalEvaluationResult(exprText, LogicalOperator.AndAlso, leftResult, rightResult, andValue, false);
    }

    static EvaluationResult AnalyzeNot(UnaryExpression unaryExpr, Dictionary<Expression, object?> cache)
    {
        var exprText = ReadableExpressionFormatter.Format(unaryExpr);
        var operandValue = GetValue(unaryExpr.Operand, cache);
        var operand = AnalyzeExpression(unaryExpr.Operand, cache);

        return new UnaryEvaluationResult(exprText, UnaryOperator.Not, operand, operandValue, !(bool)operandValue!);
    }

    static EvaluationResult AnalyzeMethodCall(MethodCallExpression methodCall, Dictionary<Expression, object?> cache)
    {
        var methodName = methodCall.Method.Name;
        var exprText = ReadableExpressionFormatter.Format(methodCall);
        var value = (bool)GetValue(methodCall, cache)!;

        if (value)
            return new ValueEvaluationResult(exprText, value, methodCall.Type);

        if (LinqOperationMethods.Contains(methodName))
            return LinqOperationFormatter.BuildResult(methodCall, exprText, value);

        if (methodName == SequenceEqualMethod)
            return SequenceEqualFormatter.BuildResult(methodCall, exprText, value);

        return new ValueEvaluationResult(exprText, value, methodCall.Type);
    }

    static object? GetValue(Expression expression, Dictionary<Expression, object?> cache)
    {
        if (cache.TryGetValue(expression, out var cached))
            return cached;

        var value = CompileAndEvaluate(expression);
        cache[expression] = value;
        return value;
    }
    
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
