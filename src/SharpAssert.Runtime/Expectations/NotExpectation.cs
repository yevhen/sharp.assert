using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert;

sealed class NotExpectation(Expectation operand) : Expectation
{
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var operandContext = context.ExprNode.Operand is { } operandNode
            ? context with { Expression = operandNode.Text, ExprNode = operandNode }
            : context;

        var operandResult = operand.Evaluate(operandContext);
        var operandValue = operandResult.BooleanValue == true;
        var value = !operandValue;

        return new UnaryEvaluationResult(context.Expression, UnaryOperator.Not, operandResult, operandValue, value);
    }
}
