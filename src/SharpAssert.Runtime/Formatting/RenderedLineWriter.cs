using System.Text;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Formatting;

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
