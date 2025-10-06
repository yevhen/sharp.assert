using System.Linq.Expressions;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class BinaryComparisonFixture : TestBase
{
    [Test]
    public void Should_show_left_and_right_values_for_equality()
    {
        var left = 42;
        var right = 24;
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows(expr, "left == right", "TestFile.cs", 123, "*42*24*");
    }

    [Test]
    public void Should_handle_equality_operator()
    {
        var left = 5;
        var right = 10;
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows(expr, "left == right", "TestFile.cs", 1, "*5*10*");
    }

    [Test]
    public void Should_handle_inequality_operator()
    {
        var left = 5;
        var right = 5;
        Expression<Func<bool>> expr = () => left != right;

        AssertExpressionThrows(expr, "left != right", "TestFile.cs", 2, "*5*5*");
    }

    [Test]
    public void Should_handle_less_than_operator()
    {
        var left = 10;
        var right = 5;
        Expression<Func<bool>> expr = () => left < right;

        AssertExpressionThrows(expr, "left < right", "TestFile.cs", 3, "*10*5*");
    }

    [Test]
    public void Should_handle_less_than_or_equal_operator()
    {
        var left = 10;
        var right = 5;
        Expression<Func<bool>> expr = () => left <= right;

        AssertExpressionThrows(expr, "left <= right", "TestFile.cs", 4, "*10*5*");
    }

    [Test]
    public void Should_handle_greater_than_operator()
    {
        var left = 5;
        var right = 10;
        Expression<Func<bool>> expr = () => left > right;

        AssertExpressionThrows(expr, "left > right", "TestFile.cs", 5, "*5*10*");
    }

    [Test]
    public void Should_handle_greater_than_or_equal_operator()
    {
        var left = 5;
        var right = 10;
        Expression<Func<bool>> expr = () => left >= right;

        AssertExpressionThrows(expr, "left >= right", "TestFile.cs", 6, "*5*10*");
    }

    [Test]
    public void Should_pass_when_all_binary_operators_are_true()
    {
        var x = 5;
        var y = 10;
        
        Expression<Func<bool>> expr1 = () => x == 5;
        Expression<Func<bool>> expr2 = () => x < y;
        Expression<Func<bool>> expr3 = () => x != y;
        Expression<Func<bool>> expr4 = () => y > x;
        Expression<Func<bool>> expr5 = () => x <= 5;
        Expression<Func<bool>> expr6 = () => y >= 10;
        
        var action1 = () => SharpInternal.Assert(expr1, "x == 5", "test.cs", 1);
        var action2 = () => SharpInternal.Assert(expr2, "x < y", "test.cs", 2);
        var action3 = () => SharpInternal.Assert(expr3, "x != y", "test.cs", 3);
        var action4 = () => SharpInternal.Assert(expr4, "y > x", "test.cs", 4);
        var action5 = () => SharpInternal.Assert(expr5, "x <= 5", "test.cs", 5);
        var action6 = () => SharpInternal.Assert(expr6, "y >= 10", "test.cs", 6);
        
        action1.Should().NotThrow("x == 5 is true");
        action2.Should().NotThrow("x < y is true");
        action3.Should().NotThrow("x != y is true");
        action4.Should().NotThrow("y > x is true");
        action5.Should().NotThrow("x <= 5 is true");
        action6.Should().NotThrow("y >= 10 is true");
    }

    [Test]
    public void Should_pass_when_string_comparison_is_true()
    {
        var str1 = "test";
        var str2 = "test";
        
        Expression<Func<bool>> expr = () => str1 == str2;
        
        var action = () => SharpInternal.Assert(expr, "str1 == str2", "test.cs", 1);
        action.Should().NotThrow("string comparison should pass when strings are equal");
    }

    int callCount;
    
    int GetValueAndIncrement()
    {
        callCount++;
        return callCount * 10;
    }

    [Test]
    public void Should_evaluate_complex_expressions_once()
    {
        callCount = 0;

        // ReSharper disable once EqualExpressionComparison
        Expression<Func<bool>> expr = () => GetValueAndIncrement() == GetValueAndIncrement();

        AssertExpressionThrows(expr, "GetValueAndIncrement() == GetValueAndIncrement()", "TestFile.cs", 200, "*");
        
        callCount.Should().Be(2, "each operand should be evaluated exactly once");
    }

    [Test]
    public void Should_handle_comparison_with_incompatible_types()
    {
        var stringValue = "hello";
        var intValue = 42;
        Expression<Func<bool>> expr = () => stringValue.Length > intValue;

        AssertExpressionThrows(expr, "stringValue.Length > intValue", "TestFile.cs", 400, "*5*42*");
    }
}