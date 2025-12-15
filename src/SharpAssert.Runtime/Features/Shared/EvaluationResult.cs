namespace SharpAssert.Features.Shared;

/// <summary>
/// Represents a single line in the rendered output of an assertion failure.
/// </summary>
/// <param name="IndentLevel">The indentation level for this line (0 = no indent).</param>
/// <param name="Text">The text content of this line.</param>
/// <remarks>
/// Used to build hierarchical output for complex assertions. Each indent level
/// typically corresponds to two spaces in the final output.
/// </remarks>
public record RenderedLine(int IndentLevel, string Text);

/// <summary>
/// Base class for all evaluation results produced by assertion expressions.
/// </summary>
/// <param name="ExpressionText">The source text of the expression that was evaluated.</param>
/// <remarks>
/// <para>
/// Each assertion expression evaluates to an <see cref="EvaluationResult"/> that captures
/// both the boolean outcome and rich diagnostic information. Subclasses implement specific
/// rendering logic for different expression types (binary comparisons, logical operations, etc.).
/// </para>
/// <para>
/// Thread Safety: Instances are immutable and thread-safe. The <see cref="Render"/> method
/// may be called concurrently from multiple threads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // When Assert(x > 10) fails with x=5:
/// var result = new BinaryComparisonEvaluationResult(
///     "x > 10",
///     ExpressionType.GreaterThan,
///     new ComparisonResult(5, 10),
///     false
/// );
///
/// // Render produces:
/// // x > 10
/// //   Left: 5
/// //   Right: 10
/// </code>
/// </example>
public abstract record EvaluationResult(string ExpressionText)
{
    /// <summary>
    /// Gets the boolean result of evaluating this expression, or null if not applicable.
    /// </summary>
    /// <remarks>
    /// Most assertion expressions evaluate to true or false, but some intermediate nodes
    /// (like value expressions) may not have a boolean interpretation.
    /// </remarks>
    public virtual bool? BooleanValue => null;

    /// <summary>
    /// Renders this evaluation result into human-readable diagnostic lines.
    /// </summary>
    /// <returns>A list of rendered lines showing why the assertion failed.</returns>
    /// <remarks>
    /// The rendering includes the expression text and detailed breakdowns of values,
    /// comparisons, and sub-expressions. Lines are returned with indentation metadata
    /// to preserve hierarchical structure in the output.
    /// </remarks>
    public abstract IReadOnlyList<RenderedLine> Render();
}
