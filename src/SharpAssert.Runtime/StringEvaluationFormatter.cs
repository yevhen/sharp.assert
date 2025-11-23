using System.Globalization;
using System.Text;

namespace SharpAssert;

class StringEvaluationFormatter : IEvaluationResultVisitor<IReadOnlyList<RenderedLine>>, IComparisonResultVisitor<IReadOnlyList<RenderedLine>>
{
    readonly string indent;
    bool suppressHeader;

    public StringEvaluationFormatter(string indent = "  ")
    {
        this.indent = indent;
    }

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

        suppressHeader = true;
        var lines = result.Result.Accept(this);
        AppendLines(sb, lines, baseIndent: 1);

        return sb.ToString().TrimEnd();
    }

    public IReadOnlyList<RenderedLine> Visit(AssertionEvaluationResult result) =>
        result.Result.Accept(this);

    public IReadOnlyList<RenderedLine> Visit(LogicalEvaluationResult result)
    {
        var lines = new List<RenderedLine>();

        // Only include the expression header when rendering as a nested block
        var wasSuppressed = suppressHeader;
        suppressHeader = false;
        var includeHeader = !wasSuppressed && result.ExpressionText is { Length: > 0 };
        if (includeHeader)
            lines.Add(new RenderedLine(0, result.ExpressionText));

        lines.AddRange(RenderLabeled(result.Left, "Left"));

        if (!result.ShortCircuited && result.Right is not null)
            lines.AddRange(RenderLabeled(result.Right, "Right"));

        lines.Add(new RenderedLine(0, GetLogicalExplanation(result)));

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(UnaryEvaluationResult result)
    {
        var wasSuppressed = suppressHeader;
        suppressHeader = false;
        var lines = new List<RenderedLine>();

        if (!wasSuppressed && result.ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, result.ExpressionText));

        lines.AddRange(RenderLabeled(result.Operand, "Operand"));
        lines.Add(new RenderedLine(0, $"!: Operand was {FormatValue(result.OperandValue)}"));

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(BinaryComparisonEvaluationResult result)
    {
        var wasSuppressed = suppressHeader;
        suppressHeader = false;
        var lines = new List<RenderedLine>();

        if (!wasSuppressed && result.ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, result.ExpressionText));

        var detailIndent = wasSuppressed ? 0 : 1;

        var comparisonLines = result.Comparison.Accept(this);
        foreach (var line in comparisonLines)
            lines.Add(new RenderedLine(detailIndent + line.IndentLevel, line.Text));

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(ValueEvaluationResult result)
    {
        suppressHeader = false;
        var valueText = FormatValue(result.Value);
        return new List<RenderedLine> { new(0, valueText) };
    }

    public IReadOnlyList<RenderedLine> Visit(FormattedEvaluationResult result)
    {
        var wasSuppressed = suppressHeader;
        suppressHeader = false;
        var lines = new List<RenderedLine>();

        if (!wasSuppressed && !string.IsNullOrEmpty(result.ExpressionText))
            lines.Add(new RenderedLine(0, result.ExpressionText));

        var detailIndent = wasSuppressed ? 0 : 1;

        foreach (var line in result.Lines)
            lines.Add(new RenderedLine(detailIndent, line));

        return lines;
    }

    static string GetLogicalExplanation(LogicalEvaluationResult result) => result.Operator switch
    {
        LogicalOperator.AndAlso when result.ShortCircuited => "&&: Left operand was false",
        LogicalOperator.AndAlso => "&&: Right operand was false",
        _ => "||: Both operands were false"
    };

    IReadOnlyList<RenderedLine> RenderLabeled(EvaluationResult child, string label)
    {
        var childLines = child.Accept(this);
        if (childLines.Count == 0)
            return Array.Empty<RenderedLine>();

        var lines = new List<RenderedLine>
        {
            new(0, $"{label}: {childLines[0].Text}")
        };

        for (var i = 1; i < childLines.Count; i++)
            lines.Add(new RenderedLine(childLines[i].IndentLevel, childLines[i].Text));

        return lines;
    }

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

    static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        DateTime dt => dt.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };

    // Comparison result visitor implementations
    public IReadOnlyList<RenderedLine> Visit(DefaultComparisonResult result)
    {
        return new List<RenderedLine>
        {
            new(0, $"Left:  {FormatValue(result.Left.Value)}"),
            new(0, $"Right: {FormatValue(result.Right.Value)}")
        };
    }

    public IReadOnlyList<RenderedLine> Visit(NullableComparisonResult result)
    {
        return new List<RenderedLine>
        {
            new(0, $"Left:  {result.LeftDisplay}"),
            new(0, $"Right: {result.RightDisplay}")
        };
    }

    public IReadOnlyList<RenderedLine> Visit(StringComparisonResult result)
    {
        return result.Lines.Select(l => new RenderedLine(0, l)).ToList();
    }

    public IReadOnlyList<RenderedLine> Visit(CollectionComparisonResult result)
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"Left:  {result.LeftPreview}"),
            new(0, $"Right: {result.RightPreview}")
        };

        if (result.FirstDifference is not null)
            lines.Add(new RenderedLine(0, result.FirstDifference));

        if (result.LengthDifference is not null)
            lines.Add(new RenderedLine(0, result.LengthDifference));

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(ObjectComparisonResult result)
    {
        return result.Lines.Select(l => new RenderedLine(0, l)).ToList();
    }

    public IReadOnlyList<RenderedLine> Visit(SequenceEqualComparisonResult result)
    {
        return result.Lines.Select(l => new RenderedLine(0, l)).ToList();
    }
}
