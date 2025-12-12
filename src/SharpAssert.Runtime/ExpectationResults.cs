using SharpAssert.Features.Shared;

namespace SharpAssert;

/// <summary>Factory helpers for building <see cref="EvaluationResult"/> instances in custom expectations.</summary>
/// <example>
/// <code>
/// using static SharpAssert.Sharp;
/// 
/// sealed class IsEven(int value) : Expectation
/// {
///     public override EvaluationResult Evaluate(ExpectationContext context) =>
///         value % 2 == 0
///             ? ExpectationResults.Boolean("value % 2 == 0", true)
///             : ExpectationResults.Fail("value % 2 == 0", $"Expected even, got {value}");
/// }
/// 
/// Assert(new IsEven(3));
/// </code>
/// </example>
public static class ExpectationResults
{
    /// <summary>Creates a successful result.</summary>
    /// <param name="expressionText">The expression text to associate with this result.</param>
    /// <returns>A successful renderable result.</returns>
    public static EvaluationResult Pass(string expressionText) =>
        new InlineExpectationResult(expressionText, true, []);

    /// <summary>Creates a boolean result with optional diagnostic lines.</summary>
    /// <param name="expressionText">The expression text to associate with this result.</param>
    /// <param name="value">The boolean outcome of the expectation.</param>
    /// <param name="lines">Optional diagnostic lines to render (shown only when the assertion fails).</param>
    /// <returns>A renderable result.</returns>
    public static EvaluationResult Boolean(string expressionText, bool value, params string[] lines) =>
        new InlineExpectationResult(expressionText, value, lines);

    /// <summary>Creates a failing result with diagnostic lines.</summary>
    /// <param name="expressionText">The expression text to associate with this result.</param>
    /// <param name="lines">Diagnostic lines to render.</param>
    /// <returns>A failing renderable result.</returns>
    public static EvaluationResult Fail(string expressionText, params string[] lines) =>
        new InlineExpectationResult(expressionText, false, lines);

    sealed record InlineExpectationResult(string ExpressionText, bool Value, IReadOnlyList<string> Lines)
        : EvaluationResult(ExpressionText)
    {
        public override bool? BooleanValue => Value;

        public override IReadOnlyList<RenderedLine> Render()
        {
            var lines = new List<RenderedLine>();

            lines.Add(new RenderedLine(0, ValueFormatter.Format(Value)));

            foreach (var line in Lines)
                lines.Add(new RenderedLine(1, line));

            return lines;
        }
    }
}
