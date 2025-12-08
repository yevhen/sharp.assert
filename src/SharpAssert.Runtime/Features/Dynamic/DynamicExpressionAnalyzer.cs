using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Dynamic;

static class DynamicExpressionAnalyzer
{
    static bool EvaluateDynamicBinaryComparison(BinaryOp op, object? leftValue, object? rightValue)
    {
        try
        {
            return op switch
            {
                BinaryOp.Eq => (dynamic?)leftValue == (dynamic?)rightValue,
                BinaryOp.Ne => (dynamic?)leftValue != (dynamic?)rightValue,
                BinaryOp.Lt => (dynamic?)leftValue < (dynamic?)rightValue,
                BinaryOp.Le => (dynamic?)leftValue <= (dynamic?)rightValue,
                BinaryOp.Gt => (dynamic?)leftValue > (dynamic?)rightValue,
                BinaryOp.Ge => (dynamic?)leftValue >= (dynamic?)rightValue,
                _ => false
            };
        }
        catch
        {
            // If dynamic operation fails, fall back to false (comparison failed)
            return false;
        }
    }

    public static AssertionEvaluationResult AnalyzeBinary(
        Func<object?> left,
        Func<object?> right,
        BinaryOp op,
        AssertionContext context)
    {
        var leftValue = left();
        var rightValue = right();

        var comparisonResult = EvaluateDynamicBinaryComparison(op, leftValue, rightValue);

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
        var result = new MethodCallEvaluationResult(context.Expression, value, []);
        return new AssertionEvaluationResult(context, result);
    }
}
