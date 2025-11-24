using System.Text;
using SharpAssert.Runtime.Comparison;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Formatting;

class StringEvaluationFormatter(string indent = "  ")
{
    public string Format(AssertionEvaluationResult result)
    {
        if (result.Passed)
            return string.Empty;

        var sb = new StringBuilder();
        var context = result.Context;
        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);

        if (context.Message is not null)
            sb.AppendLine(context.Message);

        sb.Append("Assertion failed: ");
        sb.Append(context.Expression);
        sb.Append("  at ");
        sb.AppendLine(locationPart);

        var lines = RenderEvaluation(result.Result);
        AppendLines(sb, lines, baseIndent: 1);

        return sb.ToString().TrimEnd();
    }

    IReadOnlyList<RenderedLine> RenderEvaluation(EvaluationResult result) => result switch
    {
        AssertionEvaluationResult assertion => RenderEvaluation(assertion.Result),
        LogicalEvaluationResult logical => logical.Render(RenderEvaluation),
        UnaryEvaluationResult unary => unary.Render(RenderEvaluation),
        BinaryComparisonEvaluationResult binary => binary.Render(RenderComparison),
        ValueEvaluationResult value => value.Render(),
        FormattedEvaluationResult formatted => formatted.Render(),
        _ => []
    };

    IReadOnlyList<RenderedLine> RenderComparison(ComparisonResult comparison) => comparison switch
    {
        DefaultComparisonResult defaultResult => defaultResult.Render(),
        NullableComparisonResult nullable => nullable.Render(),
        StringComparisonResult stringResult => stringResult.Render(),
        CollectionComparisonResult collection => collection.Render(),
        ObjectComparisonResult @object => @object.Render(),
        SequenceEqualComparisonResult sequence => sequence.Render(),
        _ => []
    };

    void AppendLines(StringBuilder sb, IReadOnlyList<RenderedLine> lines, int baseIndent)
    {
        foreach (var line in lines)
        {
            var totalIndent = baseIndent + line.IndentLevel;
            for (var i = 0; i < totalIndent; i++)
                sb.Append(indent);
            sb.AppendLine(line.Text);
        }
    }
}
