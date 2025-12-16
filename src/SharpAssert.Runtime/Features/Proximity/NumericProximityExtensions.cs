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
    public static NumericProximityExpectation<T> BeCloseTo<T>(this T actual, T expected, T tolerance)
        where T : struct, INumber<T>
    {
        return new NumericProximityExpectation<T>(actual, expected, tolerance);
    }

    /// <summary>Validates that a value is approximately equal to a target value.</summary>
    /// <typeparam name="T">A numeric type.</typeparam>
    /// <param name="actual">The actual value to check.</param>
    /// <param name="expected">The expected target value.</param>
    /// <param name="tolerance">The maximum allowed difference.</param>
    /// <returns>An expectation that validates proximity.</returns>
    /// <remarks>This is an alias for <see cref="BeCloseTo{T}"/> for API compatibility.</remarks>
    public static NumericProximityExpectation<T> BeApproximately<T>(this T actual, T expected, T tolerance)
        where T : struct, INumber<T>
    {
        return new NumericProximityExpectation<T>(actual, expected, tolerance);
    }
}
