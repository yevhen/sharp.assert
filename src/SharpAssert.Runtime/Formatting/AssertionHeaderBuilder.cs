using System.Text;
using SharpAssert.Runtime.Evaluation;

namespace SharpAssert.Runtime.Formatting;

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
