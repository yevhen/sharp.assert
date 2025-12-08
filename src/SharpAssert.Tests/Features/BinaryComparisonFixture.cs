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

    [Test]
    public void Should_recursively_format_nested_binary_expressions()
    {
        var x = 10;
        var y = 5;
        var z = 3;

        var action = () => Assert(x + y * z > 100);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("x + y * z");
    }

    [Test]
    public void Should_show_clean_expression_text_for_simple_comparison()
    {
        var x = 5;
        var y = 10;

        var action = () => Assert(x > y);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("x > y");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
    }

    [Test]
    public void Should_show_clean_text_for_nested_arithmetic_comparison()
    {
        var x = 2;
        var y = 3;
        var z = 5;

        var action = () => Assert(x + y * z > 100);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("x + y * z > 100");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
    }

    [Test]
    public void Should_show_original_variable_names_in_closures()
    {
        var capturedValue = 42;

        Action testAction = () =>
        {
            var localValue = 10;
            Assert(localValue > capturedValue);
        };

        testAction.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("localValue > capturedValue");
        testAction.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
        testAction.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("<>");
    }

    [Test]
    public void Should_preserve_operator_precedence_in_expression_text()
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;

        var action = () => Assert(a + b * c == d);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("a + b * c == d");
    }

    [Test]
    public void Should_show_clean_left_operand_with_nested_arithmetic()
    {
        var x = 2;
        var y = 3;
        var expected = 100;

        var action = () => Assert(x + y > expected);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("x + y");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }

    [Test]
    public void Should_show_clean_right_operand_with_nested_arithmetic()
    {
        var value = 5;
        var a = 10;
        var b = 20;

        var action = () => Assert(value > a + b);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("a + b");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }

    [Test]
    public void Should_show_clean_operands_with_complex_nested_expressions()
    {
        var x = 1;
        var y = 2;
        var z = 3;
        var a = 10;
        var b = 20;

        var action = () => Assert(x + y * z == a + b);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("x + y * z");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("a + b");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }
}