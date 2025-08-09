using System.Linq.Expressions;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class ExpressionAnalysisFixture : TestBase
{
    [Test]
    public void Should_show_left_and_right_values_for_equality()
    {
        var left = 42;
        var right = 24;
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left == right", "TestFile.cs", 123, "*42*24*");
    }

    [Test]
    public void Should_handle_equality_operator()
    {
        var left = 5;
        var right = 10;
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left == right", "TestFile.cs", 1, "*5*10*");
    }

    [Test]
    public void Should_handle_inequality_operator()
    {
        var left = 5;
        var right = 5;
        Expression<Func<bool>> expr = () => left != right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left != right", "TestFile.cs", 2, "*5*5*");
    }

    [Test]
    public void Should_handle_less_than_operator()
    {
        var left = 10;
        var right = 5;
        Expression<Func<bool>> expr = () => left < right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left < right", "TestFile.cs", 3, "*10*5*");
    }

    [Test]
    public void Should_handle_less_than_or_equal_operator()
    {
        var left = 10;
        var right = 5;
        Expression<Func<bool>> expr = () => left <= right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left <= right", "TestFile.cs", 4, "*10*5*");
    }

    [Test]
    public void Should_handle_greater_than_operator()
    {
        var left = 5;
        var right = 10;
        Expression<Func<bool>> expr = () => left > right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left > right", "TestFile.cs", 5, "*5*10*");
    }

    [Test]
    public void Should_handle_greater_than_or_equal_operator()
    {
        var left = 5;
        var right = 10;
        Expression<Func<bool>> expr = () => left >= right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left >= right", "TestFile.cs", 6, "*5*10*");
    }

    [Test]
    public void Should_handle_null_operands()
    {
        string? nullString = null;
        var value = "test";
        Expression<Func<bool>> expr = () => nullString == value;

        AssertExpressionThrows<SharpAssertionException>(expr, "nullString == value", "TestFile.cs", 100, "*null*test*");
    }

    int callCount = 0;
    
    int GetValueAndIncrement()
    {
        callCount++;
        return callCount * 10;
    }

    [Test]
    public void Should_evaluate_complex_expressions_once()
    {
        callCount = 0;

        Expression<Func<bool>> expr = () => GetValueAndIncrement() == GetValueAndIncrement();

        AssertExpressionThrows<SharpAssertionException>(expr, "GetValueAndIncrement() == GetValueAndIncrement()", "TestFile.cs", 200, "*");
        
        callCount.Should().Be(2, "each operand should be evaluated exactly once");
    }

    [Test]
    public void Should_handle_simple_boolean_property_false()
    {
        var obj = new TestObject { IsValid = false };
        Expression<Func<bool>> expr = () => obj.IsValid;

        AssertExpressionThrows<SharpAssertionException>(expr, "obj.IsValid", "TestFile.cs", 300, "Assertion failed: obj.IsValid  at TestFile.cs:300");
    }

    [Test]
    public void Should_handle_simple_boolean_method_call_false()
    {
        var obj = new TestObject { IsValid = false };
        Expression<Func<bool>> expr = () => obj.GetValidationResult();

        AssertExpressionThrows<SharpAssertionException>(expr, "obj.GetValidationResult()", "TestFile.cs", 301, "Assertion failed: obj.GetValidationResult()  at TestFile.cs:301");
    }

    [Test]
    public void Should_handle_boolean_constant_false()
    {
        Expression<Func<bool>> expr = () => false;

        AssertExpressionThrows<SharpAssertionException>(expr, "false", "TestFile.cs", 302, "Assertion failed: false  at TestFile.cs:302");
    }

    [Test]
    public void Should_handle_simple_boolean_property_true()
    {
        var obj = new TestObject { IsValid = true };
        Expression<Func<bool>> expr = () => obj.IsValid;

        AssertExpressionDoesNotThrow(expr, "obj.IsValid", "TestFile.cs", 303);
    }

    [Test]
    public void Should_handle_boolean_constant_true()
    {
        Expression<Func<bool>> expr = () => true;

        AssertExpressionDoesNotThrow(expr, "true", "TestFile.cs", 304);
    }

    [Test]
    public void Should_handle_comparison_exceptions_for_incompatible_types()
    {
        var stringValue = "hello";
        var intValue = 42;
        Expression<Func<bool>> expr = () => stringValue.Length > intValue;

        AssertExpressionThrows<SharpAssertionException>(expr, "stringValue.Length > intValue", "TestFile.cs", 400, "*5*42*");
    }

    [Test]
    public void Should_handle_comparison_exceptions_by_returning_false()
    {
        var objA = new NonComparableClass { Name = "A" };
        var objB = new DifferentNonComparableClass { Value = 10 };
        Expression<Func<bool>> expr = () => ReferenceEquals(objA, objB);

        AssertExpressionThrows<SharpAssertionException>(expr, "ReferenceEquals(objA, objB)", "TestFile.cs", 401, "*");
    }
}

