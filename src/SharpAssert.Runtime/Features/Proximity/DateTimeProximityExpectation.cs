// ABOUTME: Validates that a DateTime value is within a tolerance of an expected value
// ABOUTME: Supports DateTime and DateTimeOffset with TimeSpan tolerance

using System;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Proximity;

/// <summary>Validates that a DateTime value is within a specified tolerance of an expected value.</summary>
public sealed class DateTimeProximityExpectation : Expectation
{
    readonly DateTime actual;
    readonly DateTime expected;
    readonly TimeSpan tolerance;

    /// <summary>Creates a proximity expectation for DateTime values.</summary>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="tolerance">The maximum allowed difference.</param>
    public DateTimeProximityExpectation(DateTime actual, DateTime expected, TimeSpan tolerance)
    {
        this.actual = actual;
        this.expected = expected;
        this.tolerance = tolerance;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var difference = (actual - expected).Duration();
        var isWithinTolerance = difference <= tolerance;

        if (isWithinTolerance)
            return ExpectationResults.Pass(context.Expression);

        return ExpectationResults.Fail(
            context.Expression,
            $"Actual: {actual:O}",
            $"Expected: {expected:O}",
            $"Tolerance: {tolerance}",
            $"Difference: {difference}");
    }
}

/// <summary>Validates that a DateTimeOffset value is within a specified tolerance of an expected value.</summary>
public sealed class DateTimeOffsetProximityExpectation : Expectation
{
    readonly DateTimeOffset actual;
    readonly DateTimeOffset expected;
    readonly TimeSpan tolerance;

    /// <summary>Creates a proximity expectation for DateTimeOffset values.</summary>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="tolerance">The maximum allowed difference.</param>
    public DateTimeOffsetProximityExpectation(DateTimeOffset actual, DateTimeOffset expected, TimeSpan tolerance)
    {
        this.actual = actual;
        this.expected = expected;
        this.tolerance = tolerance;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var difference = (actual - expected).Duration();
        var isWithinTolerance = difference <= tolerance;

        if (isWithinTolerance)
            return ExpectationResults.Pass(context.Expression);

        return ExpectationResults.Fail(
            context.Expression,
            $"Actual: {actual:O}",
            $"Expected: {expected:O}",
            $"Tolerance: {tolerance}",
            $"Difference: {difference}");
    }
}
