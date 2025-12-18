// ABOUTME: Extension methods for DateTime proximity validation
// ABOUTME: Provides BeCloseTo for DateTime and DateTimeOffset types

using System;

namespace SharpAssert.Features.Proximity;

/// <summary>Provides proximity validation extensions for DateTime types.</summary>
public static class DateTimeProximityExtensions
{
    /// <summary>Validates that a DateTime is within the specified tolerance of an expected value.</summary>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected target value.</param>
    /// <param name="tolerance">The maximum allowed time difference.</param>
    /// <returns>An expectation that validates proximity.</returns>
    public static Expectation BeCloseTo(this DateTime actual, DateTime expected, TimeSpan tolerance)
    {
        return Expectation.From(
            () => (actual - expected).Duration() <= tolerance,
            () => [
                $"Actual: {actual:O}",
                $"Expected: {expected:O}",
                $"Tolerance: {tolerance}",
                $"Difference: {(actual - expected).Duration()}"
            ]
        );
    }

    /// <summary>Validates that a DateTimeOffset is within the specified tolerance of an expected value.</summary>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected target value.</param>
    /// <param name="tolerance">The maximum allowed time difference.</param>
    /// <returns>An expectation that validates proximity.</returns>
    public static Expectation BeCloseTo(this DateTimeOffset actual, DateTimeOffset expected, TimeSpan tolerance)
    {
        return Expectation.From(
            () => (actual - expected).Duration() <= tolerance,
            () => [
                $"Actual: {actual:O}",
                $"Expected: {expected:O}",
                $"Tolerance: {tolerance}",
                $"Difference: {(actual - expected).Duration()}"
            ]
        );
    }
}
