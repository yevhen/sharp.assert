// ABOUTME: Extension methods for numeric proximity validation
// ABOUTME: Provides BeCloseTo and BeApproximately for all numeric types

using System.Numerics;

namespace SharpAssert.Features.Proximity;

/// <summary>Provides proximity validation extensions for numeric types.</summary>
public static class NumericProximityExtensions
{
    /// <summary>Validates that a value is within the specified tolerance of a target value.</summary>
    /// <typeparam name="T">A numeric type.</typeparam>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected target value.</param>
    /// <param name="tolerance">The maximum allowed difference.</param>
    /// <returns>An expectation that validates proximity.</returns>
    public static Expectation BeCloseTo<T>(this T actual, T expected, T tolerance)
        where T : struct, INumber<T>
    {
        return Expectation.From(
            () => T.Abs(actual - expected) <= tolerance,
            () => [
                $"Actual: {actual}",
                $"Expected: {expected}",
                $"Tolerance: {tolerance}",
                $"Difference: {T.Abs(actual - expected)}"
            ]
        );
    }

    /// <summary>Validates that a value is approximately equal to a target value.</summary>
    /// <typeparam name="T">A numeric type.</typeparam>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected target value.</param>
    /// <param name="tolerance">The maximum allowed difference.</param>
    /// <returns>An expectation that validates proximity.</returns>
    /// <remarks>This is an alias for <see cref="BeCloseTo{T}"/> for API compatibility.</remarks>
    public static Expectation BeApproximately<T>(this T actual, T expected, T tolerance)
        where T : struct, INumber<T>
    {
        return BeCloseTo(actual, expected, tolerance);
    }
}
