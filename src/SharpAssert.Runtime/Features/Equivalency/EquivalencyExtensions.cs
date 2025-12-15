// ABOUTME: Extension methods for object equivalency assertions.
// ABOUTME: Provides IsEquivalentTo() for deep object comparison with configuration.

namespace SharpAssert;

/// <summary>
/// Provides extension methods for creating equivalency expectations.
/// </summary>
/// <remarks>
/// These extension methods enable fluent syntax for deep object comparisons in assertions.
/// Import this class with <c>using SharpAssert;</c> to access the methods.
/// </remarks>
public static class EquivalencyExtensions
{
    /// <summary>
    /// Creates an expectation that two objects are structurally equivalent with default comparison rules.
    /// </summary>
    /// <typeparam name="T">The type of objects being compared.</typeparam>
    /// <param name="actual">The actual object to compare.</param>
    /// <param name="expected">The expected object to compare against.</param>
    /// <returns>An expectation that can be used in <c>Sharp.Assert()</c>.</returns>
    /// <remarks>
    /// <para>
    /// Performs a deep comparison of all properties and fields using default rules:
    /// - All public and private members are compared
    /// - Collection order is significant
    /// - Reference types are compared by value, not reference
    /// </para>
    /// <para>
    /// For customized comparison (excluding properties, ignoring order, etc.),
    /// use the overload that accepts a configuration function.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using static SharpAssert.Sharp;
    ///
    /// var actual = new Person { Name = "Alice", Age = 30 };
    /// var expected = new Person { Name = "Alice", Age = 30 };
    ///
    /// // Basic equivalency check
    /// Assert(actual.IsEquivalentTo(expected));
    /// </code>
    /// </example>
    public static IsEquivalentToExpectation<T> IsEquivalentTo<T>(this T actual, T expected)
    {
        return new IsEquivalentToExpectation<T>(actual, expected, new EquivalencyConfig<T>());
    }

    /// <summary>
    /// Creates an expectation that two objects are structurally equivalent using custom comparison rules.
    /// </summary>
    /// <typeparam name="T">The type of objects being compared.</typeparam>
    /// <param name="actual">The actual object to compare.</param>
    /// <param name="expected">The expected object to compare against.</param>
    /// <param name="configure">Function to configure comparison rules.</param>
    /// <returns>An expectation that can be used in <c>Sharp.Assert()</c>.</returns>
    /// <remarks>
    /// <para>
    /// The configuration function receives a <see cref="EquivalencyConfig{T}"/> that you
    /// can customize using a fluent API. Common customizations include excluding properties,
    /// including only specific properties, and ignoring collection order.
    /// </para>
    /// <para>
    /// Thread Safety: The configuration is not shared between calls, so concurrent assertions
    /// are safe as long as the compared objects are not mutated during comparison.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using static SharpAssert.Sharp;
    ///
    /// var actual = new Person { Id = 1, Name = "Alice", Age = 30 };
    /// var expected = new Person { Id = 2, Name = "Alice", Age = 30 };
    ///
    /// // Exclude the Id property from comparison
    /// Assert(actual.IsEquivalentTo(expected, config =>
    ///     config.Excluding(x => x.Id)));
    ///
    /// // Multiple configuration options
    /// Assert(actual.IsEquivalentTo(expected, config =>
    ///     config.Excluding(x => x.Id)
    ///           .WithoutStrictOrdering()));
    /// </code>
    /// </example>
    public static IsEquivalentToExpectation<T> IsEquivalentTo<T>(
        this T actual,
        T expected,
        Func<EquivalencyConfig<T>, EquivalencyConfig<T>> configure)
    {
        var config = configure(new EquivalencyConfig<T>());
        return new IsEquivalentToExpectation<T>(actual, expected, config);
    }
}
