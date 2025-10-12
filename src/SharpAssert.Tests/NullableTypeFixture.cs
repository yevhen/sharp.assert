using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class NullableTypeFixture : TestBase
{
    [Test]
    public void Should_show_null_state_for_nullable_int()
    {
        int? nullableValue = null;
        var nonNullValue = 42;

        AssertThrows(() => Assert(nullableValue == nonNullValue),
            "*null*42*");
    }

    [Test]
    public void Should_show_value_state_for_nullable_int()
    {
        int? nullableValue = 42;
        var nonNullValue = 24;

        AssertThrows(() => Assert(nullableValue == nonNullValue),
            "*42*24*");
    }

    [Test]
    public void Should_show_null_state_for_nullable_bool()
    {
        bool? nullableBool = null;
        var regularBool = true;

        AssertThrows(() => Assert(nullableBool == regularBool),
            "*null*True*");
    }

    [Test]
    public void Should_show_value_state_for_nullable_bool()
    {
        bool? nullableBool = false;
        var regularBool = true;

        AssertThrows(() => Assert(nullableBool == regularBool),
            "*False*True*");
    }

    [Test]
    public void Should_show_null_state_for_nullable_DateTime()
    {
        DateTime? nullableDate = null;
        var regularDate = new DateTime(2023, 1, 1);

        AssertThrows(() => Assert(nullableDate == regularDate),
            "*null*1/1/2023*");
    }

    [Test]
    public void Should_show_value_state_for_nullable_DateTime()
    {
        DateTime? nullableDate = new DateTime(2023, 6, 15);
        var regularDate = new DateTime(2023, 1, 1);

        AssertThrows(() => Assert(nullableDate == regularDate),
            "*6/15/2023*1/1/2023*");
    }

    [Test]
    public void Should_pass_when_both_nullable_values_are_null()
    {
        int? nullable1 = null;
        int? nullable2 = null;

        AssertDoesNotThrow(() => Assert(nullable1 == nullable2));
    }

    [Test]
    public void Should_pass_when_both_nullable_values_have_same_value()
    {
        int? nullable1 = 42;
        int? nullable2 = 42;

        AssertDoesNotThrow(() => Assert(nullable1 == nullable2));
    }

    [Test]
    public void Should_show_detailed_state_for_nullable_vs_nullable()
    {
        int? nullableNull = null;
        int? nullableValue = 42;

        AssertThrows(() => Assert(nullableNull == nullableValue),
            "*null*42*");
    }

    [Test]
    public void Should_handle_nullable_reference_types()
    {
        string? nullableString = null;
        string? nonNullString = "hello";

        AssertThrows(() => Assert(nullableString == nonNullString),
            "*null*\"hello\"*");
    }

    [Test]
    public void Should_handle_nullable_object_types()
    {
        object? nullableObject = null;
        var nonNullObject = new { Name = "Test" };

        AssertThrows(() => Assert(nullableObject == nonNullObject),
            "*null*{ Name = Test }*");
    }

    [Test]
    public void Should_show_HasValue_information_in_detailed_diagnostics()
    {
        int? nullableWithoutValue = null;
        int? nullableWithValue = 42;

        AssertThrows(() => Assert(nullableWithoutValue == nullableWithValue),
            "*null*42*");
    }

    [Test]
    public void Should_handle_null_comparison_edge_cases()
    {
        int? nullable = 42;

        AssertThrows(() => Assert(nullable == null),
            "*42*null*");
    }
}