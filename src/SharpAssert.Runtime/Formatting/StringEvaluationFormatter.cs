using System.Text;
using SharpAssert.Runtime.Comparison;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Formatting;

class StringEvaluationFormatter(string indent = "  ") : IEvaluationResultVisitor<IReadOnlyList<RenderedLine>>,
    IComparisonResultVisitor<IReadOnlyList<RenderedLine>>
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

        var lines = result.Result.Accept(this);
        AppendLines(sb, lines, baseIndent: 1);

        return sb.ToString().TrimEnd();
    }

    public IReadOnlyList<RenderedLine> Visit(AssertionEvaluationResult result) =>
        result.Result.Accept(this);

    public IReadOnlyList<RenderedLine> Visit(LogicalEvaluationResult result) =>
        result.Render(RenderChild);

    public IReadOnlyList<RenderedLine> Visit(UnaryEvaluationResult result) =>
        result.Render(RenderChild);

    public IReadOnlyList<RenderedLine> Visit(BinaryComparisonEvaluationResult result) =>
        result.Render(RenderComparison);

    public IReadOnlyList<RenderedLine> Visit(ValueEvaluationResult result) =>
        result.Render();

    public IReadOnlyList<RenderedLine> Visit(FormattedEvaluationResult result) =>
        result.Render();

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

    IReadOnlyList<RenderedLine> RenderChild(EvaluationResult child) => child.Accept(this);
    IReadOnlyList<RenderedLine> RenderComparison(ComparisonResult comparison) => comparison.Accept(this);

    public IReadOnlyList<RenderedLine> Visit(DefaultComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(NullableComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(StringComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(CollectionComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(ObjectComparisonResult result)
    {
        return result.Render();
    }

    public IReadOnlyList<RenderedLine> Visit(SequenceEqualComparisonResult result)
    {
        return result.Render();
    }

}
