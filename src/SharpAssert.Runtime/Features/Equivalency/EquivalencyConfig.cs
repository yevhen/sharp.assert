// ABOUTME: Configuration builder for equivalency assertions.
// ABOUTME: Provides a fluent API for customizing object comparison behavior.

using System.Linq.Expressions;
using KellermanSoftware.CompareNetObjects;

namespace SharpAssert;

/// <summary>
/// Configures how objects are compared for equivalency assertions.
/// </summary>
/// <typeparam name="T">The type of objects being compared.</typeparam>
/// <remarks>
/// <para>
/// This configuration builder provides a fluent API to customize deep object comparisons.
/// You can exclude specific properties, include only certain properties, or control
/// how collections are compared.
/// </para>
/// <para>
/// Configuration methods are chainable, allowing multiple customizations to be applied
/// in a single expression.
/// </para>
/// <para>
/// Thread Safety: This type is not thread-safe. Create separate instances for concurrent comparisons.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Exclude a property from comparison
/// Assert(actual.IsEquivalentTo(expected, config =>
///     config.Excluding(x => x.Id)));
///
/// // Combine multiple configurations
/// Assert(actual.IsEquivalentTo(expected, config =>
///     config.Excluding(x => x.Timestamp)
///           .WithoutStrictOrdering()));
/// </code>
/// </example>
public sealed class EquivalencyConfig<T>
{
    internal ComparisonConfig ComparisonConfig { get; } = new();

    /// <summary>
    /// Excludes a property or field from the comparison.
    /// </summary>
    /// <param name="memberSelector">Expression selecting the member to exclude (e.g., x => x.Id).</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// Use this to ignore properties that should not affect equivalency, such as
    /// auto-generated IDs, timestamps, or computed values.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Exclude the Id property from comparison
    /// config.Excluding(x => x.Id);
    ///
    /// // Multiple exclusions
    /// config.Excluding(x => x.Id)
    ///       .Excluding(x => x.CreatedAt);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">
    /// Thrown when the expression does not refer to a property or field.
    /// </exception>
    public EquivalencyConfig<T> Excluding(Expression<Func<T, object>> memberSelector)
    {
        var memberName = GetMemberName(memberSelector);
        ComparisonConfig.MembersToIgnore.Add(memberName);
        return this;
    }

    /// <summary>
    /// Restricts comparison to only the specified property or field.
    /// </summary>
    /// <param name="memberSelector">Expression selecting the member to include (e.g., x => x.Name).</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// When you use <see cref="Including"/>, only explicitly included members will be compared.
    /// All other members are ignored. This is useful when you only care about a few properties
    /// in a large object graph.
    /// </para>
    /// <para>
    /// Combining <see cref="Including"/> with <see cref="Excluding"/> allows fine-grained control:
    /// include a subset of members, then exclude specific ones from that subset.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Compare only the Name property
    /// config.Including(x => x.Name);
    ///
    /// // Compare only specific properties
    /// config.Including(x => x.FirstName)
    ///       .Including(x => x.LastName);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">
    /// Thrown when the expression does not refer to a property or field.
    /// </exception>
    public EquivalencyConfig<T> Including(Expression<Func<T, object>> memberSelector)
    {
        var memberName = GetMemberName(memberSelector);
        ComparisonConfig.MembersToInclude.Add(memberName);
        return this;
    }

    /// <summary>
    /// Disables strict collection ordering, allowing collections to be equivalent regardless of element order.
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// By default, collections must have elements in the same order to be considered equivalent.
    /// This method removes that requirement, treating [1, 2, 3] as equivalent to [3, 1, 2].
    /// </para>
    /// <para>
    /// Performance: Order-independent comparison is more expensive than ordered comparison,
    /// especially for large collections. Use this only when order truly doesn't matter.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Allow any order in collections
    /// Assert(actual.IsEquivalentTo(expected, config =>
    ///     config.WithoutStrictOrdering()));
    ///
    /// // Now [1, 2, 3] is equivalent to [3, 1, 2]
    /// </code>
    /// </example>
    public EquivalencyConfig<T> WithoutStrictOrdering()
    {
        ComparisonConfig.IgnoreCollectionOrder = true;
        return this;
    }

    static string GetMemberName(Expression<Func<T, object>> expression)
    {
        if (expression.Body is MemberExpression memberExpr)
            return memberExpr.Member.Name;

        if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
            return unaryMember.Member.Name;

        throw new ArgumentException($"Expression '{expression}' does not refer to a property or field.");
    }
}
