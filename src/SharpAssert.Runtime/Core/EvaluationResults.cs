using System.Linq.Expressions;
using System.Text;
using SharpAssert.Features.Shared;

namespace SharpAssert.Core;

enum LogicalOperator
{
    AndAlso,
    OrElse
}

enum UnaryOperator
{
    Not
}

/// <summary>
/// Combines an assertion context with its evaluation result for complete failure reporting.
/// </summary>
/// <param name="Context">The assertion context containing source location and expression text.</param>
/// <param name="Result">The evaluation result containing diagnostic information.</param>
/// <remarks>
/// <para>
/// This type ties together the "where" (from <see cref="AssertionContext"/>) and the "why"
/// (from <see cref="EvaluationResult"/>) of an assertion failure. It's the top-level result
/// type that gets stored in <see cref="SharpAssertionException"/> and used to generate the
/// complete failure message.
/// </para>
/// <para>
/// Thread Safety: This type is immutable and thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // When Assert(x > 10) fails at line 42:
/// var result = new AssertionEvaluationResult(
///     new AssertionContext("x > 10", "Test.cs", 42, null, exprNode),
///     new BinaryComparisonEvaluationResult(...)
/// );
///
/// // Format() produces:
/// // Assertion failed: x > 10  at Test.cs:42
/// //   x > 10
/// //     Left: 5
/// //     Right: 10
/// </code>
/// </example>
public record AssertionEvaluationResult(AssertionContext Context, EvaluationResult Result)
    : EvaluationResult(Context.Expression)
{
    /// <summary>
    /// Gets whether the assertion passed (result was true).
    /// </summary>
    public bool Passed => Result.BooleanValue == true;

    /// <summary>
    /// Returns a bounded summary for test runners that display the exception result.
    /// </summary>
    /// <returns><c>Passed</c> or <c>Failed</c>.</returns>
    /// <remarks>
    /// This method does not render the result graph. Use <see cref="Format"/> for diagnostics
    /// or inspect <see cref="Context"/> and <see cref="Result"/> for structured data.
    /// It reads immutable state and is safe for concurrent calls.
    /// </remarks>
    /// <example>
    /// <code>var summary = assertionEvaluationResult.ToString();</code>
    /// </example>
    public override string ToString() => Passed ? "Passed" : "Failed";

    /// <summary>
    /// Gets the boolean result of the assertion.
    /// </summary>
    public override bool? BooleanValue => Result.BooleanValue;

    /// <summary>
    /// Renders the diagnostic lines from the underlying evaluation result.
    /// </summary>
    /// <returns>Rendered diagnostic lines showing why the assertion failed.</returns>
    public override IReadOnlyList<RenderedLine> Render() => Result.Render();

    /// <summary>
    /// Formats the complete assertion failure message including context and diagnostics.
    /// </summary>
    /// <param name="indent">The string to use for each indentation level (default is two spaces).</param>
    /// <returns>
    /// An empty string if the assertion passed; otherwise, a formatted multi-line message
    /// showing the assertion location, expression, and detailed diagnostic information.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method produces the final human-readable output shown in test failures.
    /// It combines the assertion header (from <see cref="AssertionContext.FormatMessage"/>)
    /// with the diagnostic details (from <see cref="EvaluationResult.Render"/>).
    /// </para>
    /// <para>
    /// Performance: String concatenation is optimized using <see cref="StringBuilder"/>.
    /// Typical formatting takes less than 1ms for moderately complex assertions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = assertionEvaluationResult;
    /// var message = result.Format();
    /// // Output:
    /// // Assertion failed: x > 10  at Test.cs:42
    /// //   x > 10
    /// //     Left: 5
    /// //     Right: 10
    /// </code>
    /// </example>
    public string Format(string indent = "  ")
    {
        if (Passed)
            return string.Empty;

        var sb = new StringBuilder(Context.FormatMessage());
        var lines = Result.Render();
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

record LogicalEvaluationResult(
    string ExpressionText,
    LogicalOperator Operator,
    EvaluationResult Left,
    EvaluationResult? Right,
    bool Value,
    bool ShortCircuited,
    ExpressionType NodeType)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        if (Operator == LogicalOperator.AndAlso && !Value)
            return RenderCondition(Left.BooleanValue != true ? Left : Right!);

        if (Operator == LogicalOperator.OrElse && Value)
            return RenderCondition(Left.BooleanValue == true ? Left : Right!);

        var summary = Operator == LogicalOperator.AndAlso
            ? "All conditions were true:"
            : "No alternative was true:";
        var lines = new List<RenderedLine> { new(0, summary) };
        lines.AddRange(RenderCondition(Left).Select(line => line with { IndentLevel = line.IndentLevel + 1 }));
        lines.AddRange(RenderCondition(Right!).Select(line => line with { IndentLevel = line.IndentLevel + 1 }));
        return lines;
    }

    static IReadOnlyList<RenderedLine> RenderCondition(EvaluationResult condition) => condition is ValueEvaluationResult value
        ? [new RenderedLine(0, $"{value.ExpressionText}: {ValueFormatter.Format(value.Value)}")]
        : condition.Render();
}

record UnaryEvaluationResult(
    string ExpressionText,
    UnaryOperator Operator,
    EvaluationResult Operand,
    object? OperandValue,
    bool Value)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>
        {
            new(0, $"{Operand.ExpressionText} was {FormatValue(OperandValue)}")
        };

        var operandLines = Operand.Render();
        var details = operandLines.FirstOrDefault()?.Text == FormatValue(OperandValue)
            ? operandLines.Skip(1)
            : operandLines;
        lines.AddRange(details.Select(line => line with { IndentLevel = line.IndentLevel + 1 }));

        return lines;
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);
}

record BinaryComparisonEvaluationResult(
    string ExpressionText,
    ExpressionType Operator,
    ComparisonResult Comparison,
    bool Value)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (ExpressionText is { Length: > 0 })
            lines.Add(new RenderedLine(0, ExpressionText));

        var comparisonLines = Comparison.Render();
        foreach (var line in comparisonLines)
            lines.Add(line with { IndentLevel = 1 + line.IndentLevel });

        return lines;
    }
}

record ValueEvaluationResult(string ExpressionText, object? Value, Type ValueType)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value as bool?;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var valueText = ValueFormatter.Format(Value);
        return new List<RenderedLine> { new(0, valueText) };
    }
}

/// <summary>
/// Represents an evaluation that already produced detail lines (e.g., LINQ/SequenceEqual).
/// </summary>
record FormattedEvaluationResult(string ExpressionText, bool Value, IReadOnlyList<string> Lines)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(ExpressionText))
            lines.Add(new RenderedLine(0, ExpressionText));

        foreach (var line in Lines)
            lines.Add(new RenderedLine(1, line));

        return lines;
    }
}

record MethodCallEvaluationResult(
    string ExpressionText,
    bool Value,
    IReadOnlyList<EvaluationResult> Arguments)
    : EvaluationResult(ExpressionText)
{
    public override bool? BooleanValue => Value;

    public override IReadOnlyList<RenderedLine> Render()
    {
        var lines = new List<RenderedLine>();

        if (!string.IsNullOrEmpty(ExpressionText))
            lines.Add(new RenderedLine(0, ExpressionText));

        for (var i = 0; i < Arguments.Count; i++)
        {
            var arg = Arguments[i];
            var argLines = arg.Render();
            if (argLines.Count == 0) continue;

            lines.Add(new RenderedLine(1, $"Argument[{i}]: {argLines[0].Text}"));

            for (var j = 1; j < argLines.Count; j++)
                lines.Add(new RenderedLine(1 + argLines[j].IndentLevel, argLines[j].Text));
        }

        lines.Add(new RenderedLine(0, $"Result: {FormatValue(Value)}"));

        return lines;
    }

    static string FormatValue(object? value) => ValueFormatter.Format(value);
}
