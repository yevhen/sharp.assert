using SharpAssert.Features.Shared;

namespace SharpAssert;

/// <summary>Base type for user-defined expectations that produce rich diagnostics.</summary>
/// <remarks>
/// <para>
/// Expectations return an <see cref="EvaluationResult"/> that captures both pass/fail and the
/// diagnostics to render on failure.
/// </para>
/// <para>
/// This is a base class (rather than only an interface) because C# does not allow implicit conversions
/// to or from interface types, and <see cref="AssertValue"/> relies on implicit conversion to keep a single
/// <see cref="Sharp.Assert(AssertValue,string?,string?,string?,int)"/> entry point.
/// </para>
/// </remarks>
public abstract class Expectation : IExpectation
{
    /// <inheritdoc />
    public abstract EvaluationResult Evaluate(ExpectationContext context);

    /// <summary>Creates an expectation that succeeds only when both operands succeed.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>A composed expectation.</returns>
    public static Expectation operator &(Expectation left, Expectation right) => new AndExpectation(left, right);

    /// <summary>Creates an expectation that succeeds when either operand succeeds.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>A composed expectation.</returns>
    public static Expectation operator |(Expectation left, Expectation right) => new OrExpectation(left, right);

    /// <summary>Creates an expectation that succeeds only when the operand fails.</summary>
    /// <param name="operand">The operand to negate.</param>
    /// <returns>A negated expectation.</returns>
    public static Expectation operator !(Expectation operand) => new NotExpectation(operand);

    /// <summary>Creates an expectation from a predicate and failure message factory.</summary>
    /// <param name="predicate">Returns true if the expectation passes.</param>
    /// <param name="onFail">Factory for diagnostic lines (only called on failure).</param>
    /// <returns>An expectation that evaluates the predicate.</returns>
    /// <example>
    /// <code>
    /// public static Expectation IsEven(this int value) =>
    ///     Expectation.From(
    ///         () => value % 2 == 0,
    ///         () => [$"Expected even number, got {value}"]
    ///     );
    /// </code>
    /// </example>
    public static Expectation From(Func<bool> predicate, Func<string[]> onFail)
        => new LambdaExpectation(predicate, onFail);

    sealed class LambdaExpectation(Func<bool> predicate, Func<string[]> onFail) : Expectation
    {
        public override EvaluationResult Evaluate(ExpectationContext context)
        {
            return predicate()
                ? ExpectationResults.Pass(context.Expression)
                : ExpectationResults.Fail(context.Expression, onFail());
        }
    }
}
