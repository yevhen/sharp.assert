using System.Collections;
using System.Linq.Expressions;
using SharpAssert.Runtime.Comparison;
using SharpAssert.Runtime.Features.SequenceEqual;
using SharpAssert.Runtime.Evaluation;
using static System.Linq.Expressions.ExpressionType;

namespace SharpAssert.Evaluation;

abstract class ExpressionAnalyzer : ExpressionVisitor
{
    static readonly string[] LinqOperationMethods = ["Contains", "Any", "All"];
    const string SequenceEqualMethod = "SequenceEqual";
    static readonly ReferenceEqualityComparer ExprComparer = ReferenceEqualityComparer.Instance;
    static readonly Dictionary<Expression, Delegate> CompiledCache = new(ExprComparer);

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

        var exprText = ExpressionDisplay.GetIdentifierOrPath(expression);
        var value = GetValue(expression, cache);

        return new ValueEvaluationResult(exprText, value, expression.Type);
    }

    static EvaluationResult AnalyzeBinaryExpression(BinaryExpression binaryExpr, Dictionary<Expression, object?> cache)
    {
        if (binaryExpr.NodeType is AndAlso or OrElse)
            return AnalyzeLogicalBinary(binaryExpr, cache);

        var leftValue = GetValue(binaryExpr.Left, cache);
        var rightValue = GetValue(binaryExpr.Right, cache);

        var leftText = ExpressionDisplay.GetIdentifierOrPath(binaryExpr.Left);
        var rightText = ExpressionDisplay.GetIdentifierOrPath(binaryExpr.Right);
        var exprText = ExpressionDisplay.FormatBinary(leftText, ExpressionDisplay.OperatorSymbol(binaryExpr.NodeType), rightText, false);

        var leftOperand = new AssertionOperand(leftValue, binaryExpr.Left.Type);
        var rightOperand = new AssertionOperand(rightValue, binaryExpr.Right.Type);
        var comparison = ComparerService.GetComparisonResult(leftOperand, rightOperand);

        var resultValue = EvaluateBinaryExpression(binaryExpr.NodeType, leftValue, rightValue);

        return new BinaryComparisonEvaluationResult(exprText, binaryExpr.NodeType, comparison, resultValue);
    }

    static EvaluationResult AnalyzeLogicalBinary(BinaryExpression binaryExpr, Dictionary<Expression, object?> cache)
    {
        var leftValue = GetValue(binaryExpr.Left, cache);
        var leftBool = (bool)leftValue!;

        if (binaryExpr.NodeType == OrElse)
        {
            var leftAnalysis = AnalyzeExpression(binaryExpr.Left, cache);
            var rightAnalysis = AnalyzeExpression(binaryExpr.Right, cache);
            var orValue = leftBool || (bool)GetValue(binaryExpr.Right, cache)!;

            var exprText = FormatLogicalText(leftAnalysis, rightAnalysis, LogicalOperator.OrElse);
            return new LogicalEvaluationResult(exprText, LogicalOperator.OrElse, leftAnalysis, rightAnalysis, orValue, false, binaryExpr.NodeType);
        }

        // AND
        if (!leftBool)
        {
            var leftAnalysis = AnalyzeExpression(binaryExpr.Left, cache);
            var rightText = ExpressionDisplay.GetIdentifierOrPath(binaryExpr.Right);
            var exprText = ExpressionDisplay.FormatBinary(leftAnalysis.ExpressionText, "&&", rightText, false);
            return new LogicalEvaluationResult(exprText, LogicalOperator.AndAlso, leftAnalysis, null, false, true, binaryExpr.NodeType);
        }

        var leftResult = AnalyzeExpression(binaryExpr.Left, cache);
        var rightResult = AnalyzeExpression(binaryExpr.Right, cache);
        var rightBool = (bool)GetValue(binaryExpr.Right, cache)!;
        var andValue = leftBool && rightBool;

        var exprTextAnd = FormatLogicalText(leftResult, rightResult, LogicalOperator.AndAlso);
        return new LogicalEvaluationResult(exprTextAnd, LogicalOperator.AndAlso, leftResult, rightResult, andValue, false, binaryExpr.NodeType);
    }

    static string FormatLogicalText(EvaluationResult left, EvaluationResult right, LogicalOperator op)
    {
        var leftText = WrapLogicalText(left, op);
        var rightText = WrapLogicalText(right, op);
        var symbol = op == LogicalOperator.AndAlso ? "&&" : "||";
        return $"{leftText} {symbol} {rightText}";
    }

    static string WrapLogicalText(EvaluationResult node, LogicalOperator parent)
    {
        if (node is LogicalEvaluationResult logical)
        {
            var childPrec = logical.Operator == LogicalOperator.AndAlso ? 2 : 1;
            var parentPrec = parent == LogicalOperator.AndAlso ? 2 : 1;
            var rendered = logical.ExpressionText;
            return childPrec < parentPrec ? $"({rendered})" : rendered;
        }

        return node.ExpressionText;
    }

    static EvaluationResult AnalyzeNot(UnaryExpression unaryExpr, Dictionary<Expression, object?> cache)
    {
        var operand = AnalyzeExpression(unaryExpr.Operand, cache);
        var exprText = ExpressionDisplay.FormatUnary("!", operand.ExpressionText, operand is LogicalEvaluationResult);
        var operandValue = GetValue(unaryExpr.Operand, cache);

        return new UnaryEvaluationResult(exprText, UnaryOperator.Not, operand, operandValue, !(bool)operandValue!);
    }

    static EvaluationResult AnalyzeMethodCall(MethodCallExpression methodCall, Dictionary<Expression, object?> cache)
    {
        var methodName = methodCall.Method.Name;
        var objectText = methodCall.Object is null ? string.Empty : ExpressionDisplay.GetIdentifierOrPath(methodCall.Object);
        var argTexts = methodCall.Arguments.Select(ExpressionDisplay.GetIdentifierOrPath);
        var exprText = ExpressionDisplay.FormatMethodCall(objectText, methodName, argTexts);
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
            compiled = Expression.Lambda(expression).Compile();
            CompiledCache[expression] = compiled;
        }

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
