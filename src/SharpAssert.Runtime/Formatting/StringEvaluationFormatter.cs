using System.Text;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Formatting;

class StringEvaluationFormatter(string indent = "  ")
{
    public string Format(AssertionEvaluationResult result)
    {
        if (result.Passed)
            return string.Empty;

        var sb = new StringBuilder(AssertionHeaderBuilder.Build(result.Context));
        var lines = result.Result.Render();
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
