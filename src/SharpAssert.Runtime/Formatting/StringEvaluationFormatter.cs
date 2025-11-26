using System.Text;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Formatting;

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
