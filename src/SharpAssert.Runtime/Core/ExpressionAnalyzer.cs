using System.Collections;
using System.Linq.Expressions;
using SharpAssert.Features.LinqOperations;
using SharpAssert.Features.SequenceEqual;
using SharpAssert.Features.Shared;
using static System.Linq.Expressions.ExpressionType;

namespace SharpAssert.Core;

abstract class ExpressionAnalyzer : ExpressionVisitor
{
    static readonly string[] LinqOperationMethods = ["Contains", "Any", "All"];
    const string SequenceEqualMethod = "SequenceEqual";
    static readonly ReferenceEqualityComparer ExprComparer = ReferenceEqualityComparer.Instance;
    static readonly Dictionary<Expression, Func<object>> CompiledCache = new(ExprComparer);

    public static string AnalyzeFailure(Expression<Func<bool>> expression, AssertionContext context)
    {
        var analysis = Analyze(expression, context);
        if (analysis.Passed)
            return string.Empty;

        return analysis.Format();
    }

    internal static AssertionEvaluationResult Analyze(Expression<Func<bool>> expression, AssertionContext context)
    {
        var cache = new Dictionary<Expression, object?>(ExprComparer);
        var result = AnalyzeExpression(expression.Body, cache, context);
        return new AssertionEvaluationResult(context, result);
    }

    static EvaluationResult AnalyzeExpression(Expression expression, Dictionary<Expression, object?> cache, AssertionContext context)
    {
        switch (expression)
        {
            case BinaryExpression binaryExpr:
                return AnalyzeBinaryExpression(binaryExpr, cache, context);
            case UnaryExpression { NodeType: Not } unaryExpr:
                return AnalyzeNot(unaryExpr, cache, context);
            case MethodCallExpression methodCall:
                return AnalyzeMethodCall(methodCall, cache, context);
        }

        var exprText = context.ExprNode.Text;
        var value = GetValue(expression, cache);

        return new ValueEvaluationResult(exprText, value, expression.Type);
    }

    static EvaluationResult AnalyzeBinaryExpression(BinaryExpression binaryExpr, Dictionary<Expression, object?> cache, AssertionContext context)
    {
        if (binaryExpr.NodeType is AndAlso or OrElse)
            return AnalyzeLogicalBinary(binaryExpr, cache, context);

        var leftValue = GetValue(binaryExpr.Left, cache);
        var rightValue = GetValue(binaryExpr.Right, cache);

        var exprText = context.ExprNode.Text;

        var leftOperand = new AssertionOperand(leftValue, binaryExpr.Left.Type);
        var rightOperand = new AssertionOperand(rightValue, binaryExpr.Right.Type);
        var comparison = ComparerService.GetComparisonResult(leftOperand, rightOperand);

        var resultValue = EvaluateBinaryExpression(binaryExpr.NodeType, leftValue, rightValue);

        return new BinaryComparisonEvaluationResult(exprText, binaryExpr.NodeType, comparison, resultValue);
    }

    static EvaluationResult AnalyzeLogicalBinary(BinaryExpression binaryExpr, Dictionary<Expression, object?> cache, AssertionContext context)
    {
        var (leftBool, leftResult) = AnalyzeLogicalOperand(binaryExpr.Left, cache, context, context.ExprNode.Left!);

        if (binaryExpr.NodeType == OrElse)
        {
            if (leftBool)
                return new LogicalEvaluationResult(context.ExprNode.Text, LogicalOperator.OrElse, leftResult, null, true, true, binaryExpr.NodeType);

            var (orRightBool, orRightResult) = AnalyzeLogicalOperand(binaryExpr.Right, cache, context, context.ExprNode.Right!);
            var orValue = leftBool || orRightBool;

            return new LogicalEvaluationResult(context.ExprNode.Text, LogicalOperator.OrElse, leftResult, orRightResult, orValue, false, binaryExpr.NodeType);
        }

        // AND
        if (!leftBool)
        {
            return new LogicalEvaluationResult(context.ExprNode.Text, LogicalOperator.AndAlso, leftResult, null, false, true, binaryExpr.NodeType);
        }

        var (andRightBool, andRightResult) = AnalyzeLogicalOperand(binaryExpr.Right, cache, context, context.ExprNode.Right!);
        var andValue = leftBool && andRightBool;

        return new LogicalEvaluationResult(context.ExprNode.Text, LogicalOperator.AndAlso, leftResult, andRightResult, andValue, false, binaryExpr.NodeType);
    }

    static (bool BoolValue, EvaluationResult Result) AnalyzeLogicalOperand(
        Expression expression,
        Dictionary<Expression, object?> cache,
        AssertionContext context,
        ExprNode exprNode)
    {
        var result = AnalyzeExpression(expression, cache, context with { ExprNode = exprNode });
        return (result.BooleanValue == true, result);
    }

    static EvaluationResult AnalyzeNot(UnaryExpression unaryExpr, Dictionary<Expression, object?> cache, AssertionContext context)
    {
        var operandResult = AnalyzeExpression(unaryExpr.Operand, cache, context with { ExprNode = context.ExprNode.Operand! });
        var operandValue = GetValue(unaryExpr.Operand, cache);

        return new UnaryEvaluationResult(context.ExprNode.Text, UnaryOperator.Not, operandResult, operandValue, !(bool)operandValue!);
    }

    static EvaluationResult AnalyzeMethodCall(MethodCallExpression methodCall, Dictionary<Expression, object?> cache, AssertionContext context)
    {
        var methodName = methodCall.Method.Name;
        var exprText = context.ExprNode.Text;
        var value = (bool)GetValue(methodCall, cache)!;

        if (value)
            return new ValueEvaluationResult(exprText, value, methodCall.Type);

        if (LinqOperationMethods.Contains(methodName))
            return LinqOperationFormatter.BuildResult(methodCall, exprText, value);

        if (methodName == SequenceEqualMethod)
        {
            var comparison = SequenceEqualComparer.BuildResult(methodCall);
            return new BinaryComparisonEvaluationResult(exprText, Equal, comparison, value);
        }

        if (context.ExprNode.Arguments is { Length: > 0 } argumentNodes)
        {
            var arguments = new List<EvaluationResult>();
            for (var i = 0; i < argumentNodes.Length; i++)
            {
                var argNode = argumentNodes[i];
                var argExpr = methodCall.Arguments[i];
                var argValue = GetValue(argExpr, cache);
                arguments.Add(new ValueEvaluationResult(argNode.Text, argValue, argExpr.Type));
            }
            return new MethodCallEvaluationResult(exprText, value, arguments);
        }

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
        if (!CompiledCache.TryGetValue(expression, out var compiled))
        {
            compiled = ExpressionValueEvaluator.Compile(expression);
            CompiledCache[expression] = compiled;
        }

        return compiled();
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
