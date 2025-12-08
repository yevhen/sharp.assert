using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Dynamic;

static class DynamicExpressionAnalyzer
{
    public static string AnalyzeDynamicBinaryFailure(
        Func<object?> left,
        Func<object?> right,
        BinaryOp op,
        AssertionContext context)
    {
        // Evaluate operands once
        var leftValue = left();
        var rightValue = right();

        var comparisonResult = EvaluateDynamicBinaryComparison(op, leftValue, rightValue);

        return comparisonResult ? string.Empty : FormatDynamicBinaryFailure(leftValue, rightValue, context);
    }

    public static string AnalyzeSimpleDynamicFailure(Func<bool> condition, AssertionContext context)
    {
        var result = condition();

        return result ? string.Empty : FormatDynamicFailure(context);
    }

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

    static string FormatBaseMessage(AssertionContext context, string? suffix = null)
    {
        var locationPart = context.FormatLocation();
        var basePart = context.Message is not null
            ? $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}\n"
            : $"Assertion failed: {context.Expression}  at {locationPart}\n";

        return suffix is not null ? basePart + suffix : basePart;
    }

    static string FormatDynamicBinaryFailure(object? leftValue, object? rightValue, AssertionContext context)
    {
        var baseMessage = FormatBaseMessage(context);
        var left = new AssertionOperand(leftValue);
        var right = new AssertionOperand(rightValue);

        var comparison = ComparerService.GetComparisonResult(left, right);
        var comparisonLines = comparison.Render();
        var details = string.Join("\n", comparisonLines.Select(l => $"  {l.Text}"));

        return string.IsNullOrEmpty(details) ? baseMessage.TrimEnd('\n') : baseMessage + details;
    }

    static string FormatDynamicFailure(AssertionContext context) => FormatBaseMessage(context, "  Result: False");

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
