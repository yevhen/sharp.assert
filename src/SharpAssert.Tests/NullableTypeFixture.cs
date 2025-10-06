using System.Linq.Expressions;

namespace SharpAssert;

[TestFixture]
public class NullableTypeFixture : TestBase
{
    [Test]
    public void Should_show_null_state_for_nullable_int()
    {
        int? nullableValue = null;
        var nonNullValue = 42;
        Expression<Func<bool>> expr = () => nullableValue == nonNullValue;

        AssertExpressionThrows(expr, "nullableValue == nonNullValue", "TestFile.cs", 123,
            "*null*42*");
    }

    [Test]
    public void Should_show_value_state_for_nullable_int()
    {
        int? nullableValue = 42;
        var nonNullValue = 24;
        Expression<Func<bool>> expr = () => nullableValue == nonNullValue;

        AssertExpressionThrows(expr, "nullableValue == nonNullValue", "TestFile.cs", 123,
            "*42*24*");
    }

    [Test]
    public void Should_show_null_state_for_nullable_bool()
    {
        bool? nullableBool = null;
        var regularBool = true;
        Expression<Func<bool>> expr = () => nullableBool == regularBool;

        AssertExpressionThrows(expr, "nullableBool == regularBool", "TestFile.cs", 123,
            "*null*True*");
    }

    [Test]
    public void Should_show_value_state_for_nullable_bool()
    {
        bool? nullableBool = false;
        var regularBool = true;
        Expression<Func<bool>> expr = () => nullableBool == regularBool;

        AssertExpressionThrows(expr, "nullableBool == regularBool", "TestFile.cs", 123,
            "*False*True*");
    }

    [Test]
    public void Should_show_null_state_for_nullable_DateTime()
    {
        DateTime? nullableDate = null;
        var regularDate = new DateTime(2023, 1, 1);
        Expression<Func<bool>> expr = () => nullableDate == regularDate;

        AssertExpressionThrows(expr, "nullableDate == regularDate", "TestFile.cs", 123,
            "*null*1/1/2023*");
    }

    [Test]
    public void Should_show_value_state_for_nullable_DateTime()
    {
        DateTime? nullableDate = new DateTime(2023, 6, 15);
        var regularDate = new DateTime(2023, 1, 1);
        Expression<Func<bool>> expr = () => nullableDate == regularDate;

        AssertExpressionThrows(expr, "nullableDate == regularDate", "TestFile.cs", 123,
            "*6/15/2023*1/1/2023*");
    }

    [Test]
    public void Should_pass_when_both_nullable_values_are_null()
    {
        int? nullable1 = null;
        int? nullable2 = null;
        Expression<Func<bool>> expr = () => nullable1 == nullable2;

        AssertExpressionPasses(expr);
    }

    [Test]
    public void Should_pass_when_both_nullable_values_have_same_value()
    {
        int? nullable1 = 42;
        int? nullable2 = 42;
        Expression<Func<bool>> expr = () => nullable1 == nullable2;

        AssertExpressionPasses(expr);
    }

    [Test]
    public void Should_show_detailed_state_for_nullable_vs_nullable()
    {
        int? nullableNull = null;
        int? nullableValue = 42;
        Expression<Func<bool>> expr = () => nullableNull == nullableValue;

        AssertExpressionThrows(expr, "nullableNull == nullableValue", "TestFile.cs", 123,
            "*null*42*");
    }

    [Test]
    public void Should_handle_nullable_reference_types()
    {
        string? nullableString = null;
        string? nonNullString = "hello";
        Expression<Func<bool>> expr = () => nullableString == nonNullString;

        AssertExpressionThrows(expr, "nullableString == nonNullString", "TestFile.cs", 123,
            "*null*\"hello\"*");
    }

    [Test]
    public void Should_handle_nullable_object_types()
    {
        object? nullableObject = null;
        var nonNullObject = new { Name = "Test" };
        Expression<Func<bool>> expr = () => nullableObject == nonNullObject;

        AssertExpressionThrows(expr, "nullableObject == nonNullObject", "TestFile.cs", 123,
            "*null*{ Name = Test }*");
    }

    [Test]
    public void Should_show_HasValue_information_in_detailed_diagnostics()
    {
        int? nullableWithoutValue = null;
        int? nullableWithValue = 42;
        Expression<Func<bool>> expr = () => nullableWithoutValue == nullableWithValue;

        AssertExpressionThrows(expr, "nullableWithoutValue == nullableWithValue", "TestFile.cs", 123,
            "*HasValue: false*HasValue: true, Value: 42*");
    }

    [Test]
    public void Should_handle_null_comparison_edge_cases()
    {
        int? nullable = 42;
        Expression<Func<bool>> expr = () => nullable == null;

        AssertExpressionThrows(expr, "nullable == null", "TestFile.cs", 123,
            "*HasValue: true, Value: 42*HasValue: false*");
    }
}