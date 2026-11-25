// ABOUTME: Extension methods for runtime type assertions.
// ABOUTME: Provides IsExactly<T>() to check the exact runtime type without accepting subclasses.

using SharpAssert.Features.Shared;

namespace SharpAssert;

/// <summary>
/// Provides runtime type expectations.
/// </summary>
public static class TypeExtensions
{
    /// <summary>Checks that the value has exactly the runtime type <typeparamref name="T"/>. Subclasses and null fail.</summary>
    /// <example><code>Assert(chart.IsExactly&lt;TextChart&gt;());</code></example>
    public static Expectation IsExactly<T>(this object? value) =>
        Expectation.From(
            () => value?.GetType() == typeof(T),
            () => [$"Expected type {typeof(T).Name}, but found {value?.GetType().Name ?? "null"}."]
        );
}
