using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class BinaryComparisonFixture : TestBase
{
    [Test]
    public void Should_show_left_and_right_values_for_equality()
    {
        var left = 42;
        var right = 24;

        AssertThrows(() => Assert(left == right), "*left == right*42*24*");
    }

    [Test]
    public void Should_handle_equality_operator()
    {
        var left = 5;
        var right = 10;

        AssertThrows(() => Assert(left == right), "*left == right*5*10*");
    }

    [Test]
    public void Should_handle_inequality_operator()
    {
        var left = 5;
        var right = 5;

        AssertThrows(() => Assert(left != right), "*left != right*5*5*");
    }

    [Test]
    public void Should_handle_less_than_operator()
    {
        var left = 10;
        var right = 5;

        AssertThrows(() => Assert(left < right), "*left < right*10*5*");
    }

    [Test]
    public void Should_handle_less_than_or_equal_operator()
    {
        var left = 10;
        var right = 5;

        AssertThrows(() => Assert(left <= right), "*left <= right*10*5*");
    }

    [Test]
    public void Should_handle_greater_than_operator()
    {
        var left = 5;
        var right = 10;

        AssertThrows(() => Assert(left > right), "*left > right*5*10*");
    }

    [Test]
    public void Should_handle_greater_than_or_equal_operator()
    {
        var left = 5;
        var right = 10;

        AssertThrows(() => Assert(left >= right), "*left >= right*5*10*");
    }

    [Test]
    public void Should_pass_when_all_binary_operators_are_true()
    {
        var x = 5;
        var y = 10;

        AssertDoesNotThrow(() => Assert(x == 5));
        AssertDoesNotThrow(() => Assert(x < y));
        AssertDoesNotThrow(() => Assert(x != y));
        AssertDoesNotThrow(() => Assert(y > x));
        AssertDoesNotThrow(() => Assert(x <= 5));
        AssertDoesNotThrow(() => Assert(y >= 10));
    }

    [Test]
    public void Should_pass_when_string_comparison_is_true()
    {
        var str1 = "test";
        var str2 = "test";

        AssertDoesNotThrow(() => Assert(str1 == str2));
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
        AssertThrows(() => Assert(GetValueAndIncrement() == GetValueAndIncrement()), "*GetValueAndIncrement() == GetValueAndIncrement()*");

        callCount.Should().Be(2, "each operand should be evaluated exactly once");
    }

    [Test]
    public void Should_handle_comparison_with_incompatible_types()
    {
        var stringValue = "hello";
        var intValue = 42;

        AssertThrows(() => Assert(stringValue.Length > intValue), "*stringValue.Length > intValue*5*42*");
    }
}