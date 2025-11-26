using System.Text;
using SharpAssert.Runtime.Comparison;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Formatting;

class StringEvaluationFormatter(string indent = "  ")
{
    readonly EvaluationRenderer renderer = new();

    public string Format(AssertionEvaluationResult result)
    {
        if (result.Passed)
            return string.Empty;

        var sb = new StringBuilder(AssertionHeaderBuilder.Build(result.Context));
        var lines = renderer.Render(result.Result);
        RenderedLineWriter.Append(sb, lines, indent, baseIndent: 1);

        return sb.ToString().TrimEnd();
    }
}

class EvaluationRenderer
{
    public IReadOnlyList<RenderedLine> Render(EvaluationResult result) => result switch
    {
        AssertionEvaluationResult assertion => Render(assertion.Result),
        LogicalEvaluationResult logical => logical.Render(Render),
        UnaryEvaluationResult unary => unary.Render(Render),
        BinaryComparisonEvaluationResult binary => binary.Render(Render),
        ValueEvaluationResult value => value.Render(),
        FormattedEvaluationResult formatted => formatted.Render(),
        _ => Array.Empty<RenderedLine>()
    };

    IReadOnlyList<RenderedLine> Render(ComparisonResult comparison) => comparison switch
    {
        DefaultComparisonResult defaultResult => defaultResult.Render(),
        NullableComparisonResult nullable => nullable.Render(),
        StringComparisonResult stringResult => stringResult.Render(),
        CollectionComparisonResult collection => collection.Render(),
        ObjectComparisonResult @object => @object.Render(),
        SequenceEqualComparisonResult sequence => sequence.Render(),
        _ => Array.Empty<RenderedLine>()
    };
}

static class AssertionHeaderBuilder
{
    public static string Build(AssertionContext context)
    {
        var sb = new StringBuilder();
        if (context.Message is not null)
            sb.AppendLine(context.Message);

        var locationPart = AssertionFormatter.FormatLocation(context.File, context.Line);
        sb.Append("Assertion failed: ");
        sb.Append(context.Expression);
        sb.Append("  at ");
        sb.AppendLine(locationPart);
        return sb.ToString();
    }
}

static class RenderedLineWriter
{
    public static void Append(StringBuilder sb, IReadOnlyList<RenderedLine> lines, string indent, int baseIndent)
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
