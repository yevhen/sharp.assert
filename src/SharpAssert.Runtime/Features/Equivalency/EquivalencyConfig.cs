// ABOUTME: Configuration builder for equivalency assertions.
// ABOUTME: Provides a fluent API for customizing object comparison behavior.

using System.Linq.Expressions;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;

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

    /// <summary>
    /// Enforces strict collection ordering during comparison (the default behavior).
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// By default, collections must have elements in the same order to be equivalent.
    /// This method explicitly enforces this behavior, useful for readability or when
    /// re-enabling strict ordering after <see cref="WithoutStrictOrdering"/> was called.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Explicitly require same element order
    /// config.WithStrictOrdering();
    ///
    /// // Now [1, 2, 3] is NOT equivalent to [3, 1, 2]
    /// </code>
    /// </example>
    public EquivalencyConfig<T> WithStrictOrdering()
    {
        ComparisonConfig.IgnoreCollectionOrder = false;
        return this;
    }

    /// <summary>
    /// Disables comparison of nested child objects.
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// By default, the comparison recursively compares all child objects.
    /// Use this method to only compare top-level properties without descending
    /// into nested object graphs.
    /// </para>
    /// <para>
    /// This is useful when you only care about simple properties and want to
    /// ignore complex nested structures, or when comparing objects with
    /// potentially infinite depth.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Compare only top-level properties, ignore nested objects
    /// Assert(actual.IsEquivalentTo(expected, config =>
    ///     config.WithoutRecursing()));
    /// </code>
    /// </example>
    public EquivalencyConfig<T> WithoutRecursing()
    {
        ComparisonConfig.CompareChildren = false;
        return this;
    }

    /// <summary>
    /// Compares instances of the specified type using Equals() instead of structural comparison.
    /// </summary>
    /// <typeparam name="TType">The type to compare by value.</typeparam>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// By default, objects are compared structurally (property by property).
    /// Use this method when a type has a meaningful Equals() implementation
    /// that should be used instead.
    /// </para>
    /// <para>
    /// This is useful for value objects, types with custom equality semantics,
    /// or types where structural comparison would be too strict or expensive.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use Money's Equals() instead of comparing Amount and Currency separately
    /// config.ComparingByValue&lt;Money&gt;();
    /// </code>
    /// </example>
    public EquivalencyConfig<T> ComparingByValue<TType>()
    {
        ComparisonConfig.CustomComparers.Add(
            new CustomComparer<TType, TType>((a, b) => a?.Equals(b) ?? b is null));
        return this;
    }

    /// <summary>
    /// Compares enum values by their string name instead of numeric value.
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// By default, enums are compared by their underlying numeric value.
    /// Use this when comparing enums from different assemblies or types
    /// that share the same member names but have different underlying values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // StatusA.Active (value 0) equals StatusB.Active (value 100) because names match
    /// config.ComparingEnumsByName();
    /// </code>
    /// </example>
    public EquivalencyConfig<T> ComparingEnumsByName()
    {
        ComparisonConfig.CustomComparers.Add(new EnumByNameComparer());
        return this;
    }

    /// <summary>
    /// Compares record types using their built-in Equals() method instead of structural comparison.
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// C# records have value-based equality by default. Use this method to leverage
    /// the record's built-in equality semantics instead of comparing each member.
    /// </para>
    /// <para>
    /// Record types are detected by checking for the compiler-generated <c>EqualityContract</c> property.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Use Point's built-in Equals() instead of comparing X and Y separately
    /// config.ComparingRecordsByValue();
    /// </code>
    /// </example>
    public EquivalencyConfig<T> ComparingRecordsByValue()
    {
        ComparisonConfig.CustomComparers.Add(new RecordByValueComparer());
        return this;
    }

    /// <summary>
    /// Registers a custom comparison function for a specific type.
    /// </summary>
    /// <typeparam name="TType">The type to apply custom comparison to.</typeparam>
    /// <param name="comparer">
    /// A function that returns <c>true</c> when two instances should be considered equal.
    /// </param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Custom comparers take precedence over the default deep comparison for the specified type.
    /// Use this when equality semantics differ from property-by-property comparison,
    /// such as comparing only specific properties or using business logic for equality.
    /// </para>
    /// <para>
    /// The comparer function receives two instances of the same type and should return
    /// <c>true</c> if they are considered equivalent, <c>false</c> otherwise.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Compare Orders only by Total, ignoring Id
    /// config.Using&lt;Order&gt;((a, b) => a.Total == b.Total);
    ///
    /// // Compare strings case-insensitively
    /// config.Using&lt;string&gt;((a, b) =>
    ///     string.Equals(a, b, StringComparison.OrdinalIgnoreCase));
    /// </code>
    /// </example>
    public EquivalencyConfig<T> Using<TType>(Func<TType, TType, bool> comparer)
    {
        ComparisonConfig.CustomComparers.Add(
            new CustomComparer<TType, TType>((a, b) => comparer(a, b)));
        return this;
    }

    /// <summary>
    /// Registers an IEqualityComparer for a specific type.
    /// </summary>
    /// <typeparam name="TType">The type to apply custom comparison to.</typeparam>
    /// <param name="comparer">The equality comparer to use for comparing instances of <typeparamref name="TType"/>.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is an alternative to <see cref="Using{TType}(Func{TType, TType, bool})"/>
    /// that accepts an <see cref="IEqualityComparer{T}"/> instead of a function.
    /// Useful when you have an existing comparer implementation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Case-insensitive string comparison
    /// config.Using(StringComparer.OrdinalIgnoreCase);
    /// </code>
    /// </example>
    public EquivalencyConfig<T> Using<TType>(IEqualityComparer<TType> comparer)
    {
        ComparisonConfig.CustomComparers.Add(
            new CustomComparer<TType, TType>((a, b) => comparer.Equals(a, b)));
        return this;
    }

    /// <summary>
    /// Excludes fields from comparison, comparing only properties.
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// By default, both fields and properties are compared. Use this method
    /// when you want to ignore all public fields and only compare properties.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Compare only properties, ignoring public fields
    /// config.ExcludingFields();
    /// </code>
    /// </example>
    public EquivalencyConfig<T> ExcludingFields()
    {
        ComparisonConfig.CompareFields = false;
        return this;
    }

    /// <summary>
    /// Explicitly includes fields in comparison (the default behavior).
    /// </summary>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Fields are included by default. This method is primarily useful for
    /// readability or to explicitly re-enable field comparison after it was disabled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Explicitly include fields (for clarity)
    /// config.IncludingFields();
    /// </code>
    /// </example>
    public EquivalencyConfig<T> IncludingFields()
    {
        ComparisonConfig.CompareFields = true;
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

sealed class EnumByNameComparer : BaseTypeComparer
{
    public EnumByNameComparer() : base(RootComparerFactory.GetRootComparer()) { }

    public override bool IsTypeMatch(Type type1, Type type2) =>
        type1.IsEnum && type2.IsEnum;

    public override void CompareType(CompareParms parms)
    {
        var name1 = parms.Object1?.ToString();
        var name2 = parms.Object2?.ToString();

        if (name1 != name2)
            AddDifference(parms);
    }
}

sealed class RecordByValueComparer : BaseTypeComparer
{
    public RecordByValueComparer() : base(RootComparerFactory.GetRootComparer()) { }

    public override bool IsTypeMatch(Type type1, Type type2) =>
        IsRecord(type1) && IsRecord(type2);

    public override void CompareType(CompareParms parms)
    {
        var areEqual = parms.Object1?.Equals(parms.Object2) ?? parms.Object2 is null;

        if (!areEqual)
            AddDifference(parms);
    }

    static bool IsRecord(Type type) =>
        type.GetProperty("EqualityContract",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) != null;
}
