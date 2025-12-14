// ABOUTME: Configuration builder for equivalency assertions.
// ABOUTME: Provides a fluent API for customizing object comparison behavior.

using System.Linq.Expressions;
using KellermanSoftware.CompareNetObjects;

namespace SharpAssert;

public sealed class EquivalencyConfig<T>
{
    internal ComparisonConfig ComparisonConfig { get; } = new();

    public EquivalencyConfig<T> Excluding(Expression<Func<T, object>> memberSelector)
    {
        var memberName = GetMemberName(memberSelector);
        ComparisonConfig.MembersToIgnore.Add(memberName);
        return this;
    }

    public EquivalencyConfig<T> Including(Expression<Func<T, object>> memberSelector)
    {
        var memberName = GetMemberName(memberSelector);
        ComparisonConfig.MembersToInclude.Add(memberName);
        return this;
    }

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
