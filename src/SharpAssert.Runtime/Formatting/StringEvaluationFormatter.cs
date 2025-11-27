using System.Text;
using SharpAssert.Runtime.Evaluation;
using SharpAssert.Runtime.Formatting;

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
        Append(sb, lines, indent, baseIndent: 1);

        return sb.ToString().TrimEnd();
    }

    static void Append(StringBuilder sb, IReadOnlyList<RenderedLine> lines, string indent, int baseIndent)
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
