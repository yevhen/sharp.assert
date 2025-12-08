using System.Collections;
using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Async;

class AsyncExpressionAnalyzer
{
    public static async Task<string> AnalyzeAsyncBinaryFailure(
        Func<Task<object?>> leftAsync,
        Func<Task<object?>> rightAsync,
        BinaryOp op,
        AssertionContext context)
    {
        // Evaluate operands in source order (left then right)
        var leftValue = await leftAsync();
        var rightValue = await rightAsync();

        var comparisonResult = EvaluateBinaryComparison(op, leftValue, rightValue);

        return comparisonResult ? string.Empty : FormatAsyncBinaryFailure(leftValue, rightValue, context);
    }

    public static async Task<string> AnalyzeSimpleAsyncFailure(
        Func<Task<bool>> conditionAsync,
        AssertionContext context)
    {
        var result = await conditionAsync();

        return result ? string.Empty : FormatAsyncFailure(context);
    }

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

    static string FormatBaseMessage(AssertionContext context, string? suffix = null)
    {
        var locationPart = context.FormatLocation();
        var basePart = context.Message is not null
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";

        return suffix is not null ? basePart + suffix : basePart;
    }

    static string FormatAsyncBinaryFailure(object? leftValue, object? rightValue, AssertionContext context)
    {
        var baseMessage = FormatBaseMessage(context);
        var left = new AssertionOperand(leftValue);
        var right = new AssertionOperand(rightValue);

        var comparison = ComparerService.GetComparisonResult(left, right);
        var comparisonLines = comparison.Render();
        var details = string.Join("\n", comparisonLines.Select(l => $"  {l.Text}"));

        return string.IsNullOrEmpty(details) ? baseMessage.TrimEnd('\n') : baseMessage + details;
    }

    static string FormatAsyncFailure(AssertionContext context) => FormatBaseMessage(context, "  Result: False");

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
        var result = new ValueEvaluationResult(context.Expression, value, typeof(bool));
        return new AssertionEvaluationResult(context, result);
    }
}
