// ABOUTME: Validates that a numeric value is within a tolerance of an expected value
// ABOUTME: Supports BeCloseTo and BeApproximately for double, float, decimal, and integer types

using System;
using System.Numerics;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features.Proximity;

/// <summary>Validates that a numeric value is within a specified tolerance of an expected value.</summary>
/// <typeparam name="T">A numeric type that supports subtraction and comparison.</typeparam>
public sealed class NumericProximityExpectation<T> : Expectation
    where T : struct, INumber<T>
{
    readonly T actual;
    readonly T expected;
    readonly T tolerance;

    /// <summary>Creates a proximity expectation for numeric values.</summary>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected value.</param>
    /// <param name="tolerance">The maximum allowed difference.</param>
    public NumericProximityExpectation(T actual, T expected, T tolerance)
    {
        this.actual = actual;
        this.expected = expected;
        this.tolerance = tolerance;
    }

    /// <inheritdoc />
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        var difference = T.Abs(actual - expected);
        var isWithinTolerance = difference <= tolerance;

        if (isWithinTolerance)
            return ExpectationResults.Pass(context.Expression);

        return ExpectationResults.Fail(
            context.Expression,
            $"Actual: {actual}",
            $"Expected: {expected}",
            $"Tolerance: {tolerance}",
            $"Difference: {difference}");
    }
}
