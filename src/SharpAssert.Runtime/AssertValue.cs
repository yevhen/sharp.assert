namespace SharpAssert;

/// <summary>Represents a value that can be asserted via <see cref="Sharp.Assert(AssertValue,string?,string?,string?,int)"/>.</summary>
/// <remarks>
/// <para>
/// This is a discriminated union used to keep a single <c>Assert(...)</c> entry point while supporting both
/// boolean assertions and user-defined expectations.
/// </para>
/// </remarks>
public readonly struct AssertValue
{
    readonly bool? condition;
    readonly Expectation? expectation;

    AssertValue(bool condition)
    {
        this.condition = condition;
        expectation = null;
    }

    AssertValue(Expectation expectation)
    {
        this.expectation = expectation;
        condition = null;
    }

    internal bool IsCondition => condition is not null;
    internal bool Condition => condition ?? false;

    internal bool IsExpectation => expectation is not null;
    internal Expectation Expectation => expectation!;

    /// <summary>Implicitly wraps a boolean condition into an <see cref="AssertValue"/>.</summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>An <see cref="AssertValue"/> representing a boolean assertion.</returns>
    public static implicit operator AssertValue(bool condition) => new(condition);

    /// <summary>Implicitly wraps an expectation into an <see cref="AssertValue"/>.</summary>
    /// <param name="expectation">The expectation instance.</param>
    /// <returns>An <see cref="AssertValue"/> representing an expectation assertion.</returns>
    public static implicit operator AssertValue(Expectation expectation) => new(expectation);
}
