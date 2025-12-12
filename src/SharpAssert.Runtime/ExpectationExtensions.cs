namespace SharpAssert;

/// <summary>Provides composition helpers for <see cref="IExpectation"/>.</summary>
/// <remarks>
/// <para>
/// These helpers compose expectations without involving expression trees, enabling custom assertion
/// extensions to build up complex checks with deterministic evaluation and rendering.
/// </para>
/// </remarks>
public static class ExpectationExtensions
{
    /// <summary>Creates an expectation that succeeds only when both operands succeed.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>A composed expectation.</returns>
    public static Expectation And(this Expectation left, Expectation right) =>
        new AndExpectation(left, right);

    /// <summary>Creates an expectation that succeeds when either operand succeeds.</summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>A composed expectation.</returns>
    public static Expectation Or(this Expectation left, Expectation right) =>
        new OrExpectation(left, right);

    /// <summary>Creates an expectation that succeeds only when the operand fails.</summary>
    /// <param name="operand">The operand to negate.</param>
    /// <returns>A negated expectation.</returns>
    public static Expectation Not(this Expectation operand) =>
        new NotExpectation(operand);
}
