using System.Linq;

namespace SharpAssert;

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
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
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
        var formatter = new StringEvaluationFormatter();
        var comparisonLines = comparison.Accept(formatter);
        var details = string.Join("\n", comparisonLines.Select(l => $"  {l.Text}"));

        return string.IsNullOrEmpty(details) ? baseMessage.TrimEnd('\n') : baseMessage + details;
    }

    static string FormatDynamicFailure(AssertionContext context) => FormatBaseMessage(context, "  Result: False");
}
