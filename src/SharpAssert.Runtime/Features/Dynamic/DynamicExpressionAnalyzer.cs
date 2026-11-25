using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Dynamic;

static class DynamicExpressionAnalyzer
{
    public static AssertionEvaluationResult AnalyzeBinary(
        Func<object?> left,
        Func<object?> right,
        BinaryOp op,
        AssertionContext context)
    {
        var leftValue = left();
        var rightValue = right();

        var comparisonResult = NativeOperators.Evaluate(op, leftValue, rightValue);

        if (comparisonResult)
            return new AssertionEvaluationResult(context, new ValueEvaluationResult(context.Expression, true, typeof(bool)));

        var leftOperand = new AssertionOperand(leftValue);
        var rightOperand = new AssertionOperand(rightValue);
        var comparison = ComparerService.GetComparisonResult(leftOperand, rightOperand);

        var result = new BinaryComparisonEvaluationResult(context.Expression, ExpressionType.Equal, comparison, false);
        return new AssertionEvaluationResult(context, result);
    }

    public static AssertionEvaluationResult Analyze(
        Func<bool> condition,
        AssertionContext context)
    {
        var value = condition();
        var result = new ValueEvaluationResult(context.Expression, value, typeof(bool));
        return new AssertionEvaluationResult(context, result);
    }
}
