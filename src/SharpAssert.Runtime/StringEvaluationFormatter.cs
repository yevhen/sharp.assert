using System.Text;
using System.Linq;
using DiffPlex.DiffBuilder.Model;

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

    static string FormatValue(object? value) => ValueFormatter.Format(value);

    // Comparison result visitor implementations
    public IReadOnlyList<RenderedLine> Visit(DefaultComparisonResult result)
    {
        return new List<RenderedLine>
        {
            new(0, $"Left:  {FormatValue(result.LeftOperand.Value)}"),
            new(0, $"Right: {FormatValue(result.RightOperand.Value)}")
        };
    }

    public IReadOnlyList<RenderedLine> Visit(NullableComparisonResult result)
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"Left:  {FormatNullableValue(result.LeftValue, result.LeftIsNull, result.LeftExpressionType)}"),
            new(0, $"Right: {FormatNullableValue(result.RightValue, result.RightIsNull, result.RightExpressionType)}")
        };
        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(StringComparisonResult result)
    {
        var lines = new List<RenderedLine>();

        if (result.Diff is InlineStringDiff inline)
        {
            lines.Add(new RenderedLine(0, $"Left:  {FormatStringValue(result.LeftText)}"));
            lines.Add(new RenderedLine(0, $"Right: {FormatStringValue(result.RightText)}"));
            var diffText = RenderInlineDiff(inline.Segments);
            lines.Add(new RenderedLine(0, $"Diff: {diffText}"));
            return lines;
        }

        if (result.Diff is MultilineStringDiff multi)
        {
            lines.Add(new RenderedLine(0, "Left:"));
            foreach (var line in (result.LeftText ?? string.Empty).Split('\n'))
                lines.Add(new RenderedLine(1, line));

            lines.Add(new RenderedLine(0, "Right:"));
            foreach (var line in (result.RightText ?? string.Empty).Split('\n'))
                lines.Add(new RenderedLine(1, line));

            lines.Add(new RenderedLine(0, "Diff:"));
            foreach (var diffLine in multi.Lines)
                lines.Add(new RenderedLine(1, RenderMultilineDiffLine(diffLine)));
        }

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(CollectionComparisonResult result)
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"Left:  {FormatCollection(result.LeftPreview)}"),
            new(0, $"Right: {FormatCollection(result.RightPreview)}")
        };

        if (result.FirstDifference is not null)
            lines.Add(new RenderedLine(0,
                $"First difference at index {result.FirstDifference.Index}: expected {FormatValue(result.FirstDifference.LeftValue)}, got {FormatValue(result.FirstDifference.RightValue)}"));

        if (result.LengthDifference is not null)
        {
            if (result.LengthDifference.Extra is not null)
                lines.Add(new RenderedLine(0, $"Extra elements: {FormatCollection(result.LengthDifference.Extra)}"));
            if (result.LengthDifference.Missing is not null)
                lines.Add(new RenderedLine(0, $"Missing elements: {FormatCollection(result.LengthDifference.Missing)}"));
        }

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(ObjectComparisonResult result)
    {
        var lines = new List<RenderedLine>();

        if (result.Differences.Count == 0)
            return lines;

        lines.Add(new RenderedLine(0, "Property differences:"));

        foreach (var diff in result.Differences)
        {
            lines.Add(new RenderedLine(1,
                $"{diff.Path}: expected {FormatValue(diff.Expected)}, got {FormatValue(diff.Actual)}"));
        }

        if (result.TruncatedCount > 0)
            lines.Add(new RenderedLine(1, $"... ({result.TruncatedCount} more differences)"));

        return lines;
    }

    public IReadOnlyList<RenderedLine> Visit(SequenceEqualComparisonResult result)
    {
        var lines = new List<RenderedLine>();

        if (result.Error is not null)
        {
            lines.Add(new RenderedLine(0, result.Error));
            return lines;
        }

        if (result.LengthMismatch is not null)
        {
            lines.Add(new RenderedLine(0, "SequenceEqual failed: length mismatch"));
            lines.Add(new RenderedLine(0, $"Expected length: {result.LengthMismatch.ExpectedLength}"));
            lines.Add(new RenderedLine(0, $"Actual length:   {result.LengthMismatch.ActualLength}"));
            lines.Add(new RenderedLine(0, $"First:  {FormatCollection(result.LengthMismatch.FirstPreview)}"));
            lines.Add(new RenderedLine(0, $"Second: {FormatCollection(result.LengthMismatch.SecondPreview)}"));
            return lines;
        }

        lines.Add(new RenderedLine(0, "SequenceEqual failed: sequences differ"));
        if (result.HasComparer)
            lines.Add(new RenderedLine(0, "(using custom comparer)"));

        lines.Add(new RenderedLine(0, "Unified diff:"));
        if (result.DiffLines is not null)
        {
            foreach (var diff in result.DiffLines)
                lines.Add(new RenderedLine(1, RenderSequenceDiffLine(diff)));
        }

        if (result.DiffTruncated)
            lines.Add(new RenderedLine(1, "... (diff truncated)"));

        return lines;
    }

    static string FormatNullableValue(object? value, bool isNull, Type? expressionType)
    {
        if (isNull)
            return "null";

        return expressionType is not null
            ? ValueFormatter.FormatWithType(value, expressionType)
            : ValueFormatter.Format(value);
    }

    static string FormatStringValue(string? value) => value == null ? "null" : $"\"{value}\"";

    static string RenderInlineDiff(IReadOnlyList<DiffSegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var segment in segments)
        {
            sb.Append(segment.Operation switch
            {
                StringDiffOperation.Deleted => $"[-{segment.Text}]",
                StringDiffOperation.Inserted => $"[+{segment.Text}]",
                _ => segment.Text
            });
        }
        return sb.ToString();
    }

    static string RenderMultilineDiffLine(TextDiffLine line) => line.Type switch
    {
        ChangeType.Unchanged => line.Text,
        ChangeType.Deleted => $"- {line.Text}",
        ChangeType.Inserted => $"+ {line.Text}",
        ChangeType.Modified => $"~ {line.Text}",
        _ => line.Text
    };

    static string FormatCollection(IReadOnlyList<object?> items)
    {
        if (items.Count == 0)
            return "[]";

        return $"[{string.Join(", ", items.Select(FormatValue))}]";
    }

    static string RenderSequenceDiffLine(SequenceDiffLine diff) => diff.Operation switch
    {
        SequenceDiffOperation.Added => $"+[{diff.Index}] {diff.Value}",
        SequenceDiffOperation.Removed => $"-[{diff.Index}] {diff.Value}",
        _ => $" [{diff.Index}] {diff.Value}"
    };
}
