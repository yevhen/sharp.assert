using System.Text;

namespace SharpAssert;

record AssertionContext(string Expression, string File, int Line, string? Message)
{
    public string FormatMessage()
    {
        var sb = new StringBuilder();
        if (Message is not null)
            sb.AppendLine(Message);

        var locationPart = AssertionFormatter.FormatLocation(File, Line);
        sb.Append("Assertion failed: ");
        sb.Append(Expression);
        sb.Append("  at ");
        sb.AppendLine(locationPart);
        return sb.ToString();
    }
}