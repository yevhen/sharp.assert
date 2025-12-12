using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert;

sealed class OrExpectation(Expectation left, Expectation right) : Expectation
{
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var (leftNode, rightNode) = GetOperandNodes(context.ExprNode);
        var leftContext = leftNode is not null
            ? context with { Expression = leftNode.Text, ExprNode = leftNode }
            : context;

        var leftResult = left.Evaluate(leftContext);
        if (leftResult.BooleanValue == true)
            return ExpectationResults.Pass(context.Expression);

        var rightContext = rightNode is not null
            ? context with { Expression = rightNode.Text, ExprNode = rightNode }
            : context;

        var rightResult = right.Evaluate(rightContext);
        if (rightResult.BooleanValue == true)
            return ExpectationResults.Pass(context.Expression);

        return new ComposedExpectationEvaluationResult(context.Expression, "OR", leftResult, rightResult, false, false);
    }

    static (ExprNode? Left, ExprNode? Right) GetOperandNodes(ExprNode node)
    {
        if (node.Left is not null && node.Right is not null)
            return (node.Left, node.Right);

        if (node.Left is not null && node.Arguments is { Length: >= 1 })
            return (node.Left, node.Arguments[0]);

        if (node.Arguments is { Length: >= 2 })
            return (node.Arguments[0], node.Arguments[1]);

        return (null, null);
    }
}
