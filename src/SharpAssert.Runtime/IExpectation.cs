using SharpAssert.Features.Shared;

namespace SharpAssert;

/// <summary>Represents a user-defined assertion that can be evaluated and rendered.</summary>
/// <remarks>
/// <para>
/// Expectations return an <see cref="EvaluationResult"/> that captures both pass/fail and the
/// diagnostics to render on failure.
/// </para>
/// <para>
/// The returned result should set <see cref="EvaluationResult.BooleanValue"/> to <c>true</c> on success.
/// Any other value (including <c>false</c> or <c>null</c>) is treated as a failure by <see cref="Sharp"/>.
/// </para>
/// </remarks>
public interface IExpectation
{
    /// <summary>Evaluates the expectation and returns a renderable result.</summary>
    /// <param name="context">Call-site information used for diagnostics.</param>
    /// <returns>
    /// A renderable result. <see cref="EvaluationResult.BooleanValue"/> should be <c>true</c> for success.
    /// </returns>
    EvaluationResult Evaluate(ExpectationContext context);
}

