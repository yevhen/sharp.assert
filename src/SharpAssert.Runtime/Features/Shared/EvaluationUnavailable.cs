namespace SharpAssert.Features.Shared;

/// <summary>
/// Represents a value that could not be evaluated at runtime.
/// </summary>
/// <param name="Reason">The reason why evaluation was not possible.</param>
/// <remarks>
/// <para>
/// Some expressions cannot be evaluated safely at runtime due to limitations in
/// expression tree compilation, platform differences, or unsupported language features.
/// When this occurs, SharpAssert uses <see cref="EvaluationUnavailable"/> as a sentinel
/// value instead of throwing exceptions or showing misleading data.
/// </para>
/// <para>
/// Common scenarios include:
/// - Span&lt;T&gt; and ReadOnlySpan&lt;T&gt; (cannot be boxed or compiled to expression trees)
/// - Expression compilation failures on certain runtimes
/// - Unsupported expression node types in custom predicates
/// </para>
/// <para>
/// When displayed in assertion failures, these appear as "&lt;unavailable: reason&gt;"
/// to make it clear that the value couldn't be captured, not that it was null or empty.
/// </para>
/// <para>
/// Thread Safety: This type is immutable and thread-safe.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // When trying to evaluate a Span&lt;int&gt; in a LINQ predicate:
/// var unavailable = new EvaluationUnavailable("Span cannot be boxed");
/// Console.WriteLine(unavailable.ToString());
/// // Output: &lt;unavailable: Span cannot be boxed&gt;
/// </code>
/// </example>
public sealed record EvaluationUnavailable(string Reason)
{
    /// <summary>
    /// Formats this unavailable value for display in assertion failures.
    /// </summary>
    /// <returns>A string in the format "&lt;unavailable: reason&gt;".</returns>
    public override string ToString() => $"<unavailable: {Reason}>";
}
