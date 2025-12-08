using System.Collections;
using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Async;

class AsyncExpressionAnalyzer
{
    static bool EvaluateBinaryComparison(BinaryOp op, object? leftValue, object? rightValue) => op switch
    {
        BinaryOp.Eq => Equals(leftValue, rightValue),
        BinaryOp.Ne => !Equals(leftValue, rightValue),
        BinaryOp.Lt => Comparer.Default.Compare(leftValue, rightValue) < 0,
        BinaryOp.Le => Comparer.Default.Compare(leftValue, rightValue) <= 0,
        BinaryOp.Gt => Comparer.Default.Compare(leftValue, rightValue) > 0,
        BinaryOp.Ge => Comparer.Default.Compare(leftValue, rightValue) >= 0,
        _ => false
    };

    public static async Task<AssertionEvaluationResult> AnalyzeBinary(
        Func<Task<object?>> leftAsync,
        Func<Task<object?>> rightAsync,
        BinaryOp op,
        AssertionContext context)
    {
        var leftValue = await leftAsync();
        var rightValue = await rightAsync();

        var comparisonResult = EvaluateBinaryComparison(op, leftValue, rightValue);

        if (comparisonResult)
            return new AssertionEvaluationResult(context, new ValueEvaluationResult(context.Expression, true, typeof(bool)));

        var leftOperand = new AssertionOperand(leftValue);
        var rightOperand = new AssertionOperand(rightValue);
        var comparison = ComparerService.GetComparisonResult(leftOperand, rightOperand);

        var result = new BinaryComparisonEvaluationResult(context.Expression, ExpressionType.Equal, comparison, false);
        return new AssertionEvaluationResult(context, result);
    }

    public static async Task<AssertionEvaluationResult> AnalyzeSimple(
        Func<Task<bool>> conditionAsync,
        AssertionContext context)
    {
        var value = await conditionAsync();
        var result = new MethodCallEvaluationResult(context.Expression, value, []);
        return new AssertionEvaluationResult(context, result);
    }
}
