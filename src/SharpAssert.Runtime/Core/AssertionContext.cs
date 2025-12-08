using System.Text;

namespace SharpAssert.Core;

record AssertionContext(string Expression, string File, int Line, string? Message, ExprNode ExprNode)
{
    public string FormatMessage()
    {
        var sb = new StringBuilder();
        if (Message is not null)
            sb.AppendLine(Message);

        var locationPart = FormatLocation();
        sb.Append("Assertion failed: ");
        sb.Append(Expression);
        sb.Append("  at ");
        sb.AppendLine(locationPart);
        return sb.ToString();
    }

    public string FormatLocation() => $"{File}:{Line}";
}