using SharpAssert.Core;

namespace SharpAssert;

/// <summary>
/// Exception thrown when a SharpAssert assertion fails.
/// </summary>
/// <remarks>
/// <para>
/// This exception is thrown by <c>Sharp.Assert()</c> when an assertion evaluates to false.
/// It contains rich diagnostic information about why the assertion failed, including
/// source location, expression text, and detailed value comparisons.
/// </para>
/// <para>
/// The exception message is formatted to show the complete context of the failure,
/// making it easy to understand what went wrong directly from test output.
/// </para>
/// <para>
/// Thread Safety: This type is thread-safe for reading after construction.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     Sharp.Assert(x > 10);
/// }
/// catch (SharpAssertionException ex)
/// {
///     // Message contains:
///     // Assertion failed: x > 10  at Test.cs:42
///     //   x > 10
///     //     Left: 5
///     //     Right: 10
///
///     // Access structured result:
///     if (ex.Result != null)
///     {
///         var passed = ex.Result.Passed; // false
///         var lines = ex.Result.Render();
///     }
/// }
/// </code>
/// </example>
public class SharpAssertionException : Exception
{
    /// <summary>
    /// Gets the detailed evaluation result that caused this assertion to fail.
    /// </summary>
    /// <remarks>
    /// This property contains the complete diagnostic information including the
    /// assertion context and evaluation tree. It's null only for assertions that
    /// fail without producing a structured result (rare edge cases).
    /// </remarks>
    public AssertionEvaluationResult? Result { get; }

    /// <summary>
    /// Initializes a new assertion exception with a simple message.
    /// </summary>
    /// <param name="message">The failure message to display.</param>
    /// <remarks>
    /// This constructor is used for basic assertion failures without structured results.
    /// Prefer using the constructor with <see cref="AssertionEvaluationResult"/> for
    /// rich diagnostic output.
    /// </remarks>
    public SharpAssertionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new assertion exception with a message and structured result.
    /// </summary>
    /// <param name="message">The formatted failure message.</param>
    /// <param name="result">The evaluation result containing diagnostic details.</param>
    /// <remarks>
    /// This is the primary constructor used by SharpAssert for assertion failures.
    /// The message is typically generated from the result using
    /// <see cref="AssertionEvaluationResult.Format"/>.
    /// </remarks>
    public SharpAssertionException(string message, AssertionEvaluationResult result) : base(message)
    {
        Result = result;
    }
}