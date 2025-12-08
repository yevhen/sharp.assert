using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class LogicalOperatorFixture : TestBase
{
    [Test]
    public void Should_show_which_part_of_AND_failed()
    {
        var left = true;
        var right = false;

        AssertThrows(() => Assert(left && right), "*&&*true*false*");
    }

    [Test]
    public void Should_short_circuit_AND_correctly()
    {
        var left = false;

        AssertThrows(() => Assert(left && ThrowException()), "*&&*false*");
    }

    [Test]
    public void Should_show_which_part_of_OR_failed()
    {
        var left = false;
        var right = false;

        AssertThrows(() => Assert(left || right), "*||*false*false*");
    }

    [Test]
    public void Should_pass_when_OR_succeeds()
    {
        var left = false;
        var right = true;

        AssertDoesNotThrow(() => Assert(left || right));
    }

    [Test]
    public void Should_pass_when_AND_succeeds()
    {
        var left = true;
        var right = true;

        AssertDoesNotThrow(() => Assert(left && right));
    }

    [Test]
    public void Should_pass_when_NOT_succeeds()
    {
        var operand = false;

        AssertDoesNotThrow(() => Assert(!operand));
    }

    [Test]
    public void Should_handle_NOT_operator()
    {
        var operand = true;

        AssertThrows(() => Assert(!operand), "*!*true*");
    }

    [Test]
    public void Should_pass_when_left_operand_of_OR_is_true()
    {
        var leftTrue = true;
        var rightFalse = false;

        AssertDoesNotThrow(() => Assert(leftTrue || rightFalse));
    }

    [Test]
    public void Should_pass_when_right_operand_of_OR_is_true()
    {
        var leftFalse = false;
        var rightTrue = true;

        AssertDoesNotThrow(() => Assert(leftFalse || rightTrue));
    }

    [Test]
    public void Should_fail_when_left_operand_of_AND_is_false()
    {
        var leftFalse = false;
        var rightTrue = true;

        AssertThrows(() => Assert(leftFalse && rightTrue), "*&&*false*");
    }

    [Test]
    public void Should_fail_when_right_operand_of_AND_is_false()
    {
        var leftTrue = true;
        var rightFalse = false;

        AssertThrows(() => Assert(leftTrue && rightFalse), "*&&*true*false*");
    }

    [Test]
    public void Should_recursively_analyze_AND_sub_expressions_with_short_circuit()
    {
        var x = 3;
        var y = 12;

        AssertThrows(() => Assert(x == 5 && y == 10), "*Left: *== 5*Left:  3*Right: 5*&&: Left operand was false*");
    }

    [Test]
    public void Should_recursively_analyze_AND_when_right_fails()
    {
        var x = 5;
        var y = 12;

        AssertThrows(() => Assert(x == 5 && y == 10), "*Left: *== 5*Left:  5*Right: 5*Right: *== 10*Left:  12*Right: 10*&&: Right operand was false*");
    }

    [Test]
    public void Should_recursively_analyze_OR_sub_expressions()
    {
        var x = 3;
        var y = 12;

        AssertThrows(() => Assert(x == 5 || y == 10), "*Left: *== 5*Left:  3*Right: 5*Right: *== 10*Left:  12*Right: 10*||: Both operands were false*");
    }

    [Test]
    public void Should_recursively_analyze_NOT_sub_expressions()
    {
        var x = 3;

        AssertThrows(() => Assert(!(x == 3)), "*Operand: *== 3*Left:  3*Right: 3*!: Operand was*");
    }

    [Test]
    public void Should_recursively_analyze_nested_logical_operators()
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;

        AssertThrows(() => Assert((a == 1 && (b == 3 || b == 8)) || (c == 3 && d == 5)),
            "*||*" +                                        // Top-level OR operator
            "*Left: (a == 1 && (b == 3 || b == 8))*" +      // Left side - preserves source parens
            "*Left: a == 1*" +                              // First operand of AND
            "*Left:  1*Right: 1*" +                         // Values of a == 1
            "*Right: (b == 3 || b == 8)*" +                 // Second operand - nested OR with source parens
            "*Left: b == 3*" +                              // Left of nested OR
            "*Left:  2*Right: 3*" +                         // Values of b == 3
            "*Right: b == 8*" +                             // Right of nested OR
            "*Left:  2*Right: 8*" +                         // Values of b == 8
            "*||: Both operands were false*" +              // Nested OR failed
            "*&&: Right operand was false*" +               // Left AND failed
            "*Right: (c == 3 && d == 5)*" +                 // Right side - preserves source parens
            "*Left: c == 3*" +                              // First operand
            "*Left:  3*Right: 3*" +                         // Values of c == 3
            "*Right: d == 5*" +                             // Second operand
            "*Left:  4*Right: 5*" +                         // Values of d == 5
            "*&&: Right operand was false*" +               // Right AND failed
            "*||: Both operands were false*");              // Top-level OR failed
    }

    [Test]
    public void Should_show_clean_expression_text_for_NOT_operator()
    {
        var value = true;

        var action = () => Assert(!value);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("!value");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }

    [Test]
    public void Should_show_clean_expression_text_for_NOT_with_comparison()
    {
        var x = 5;
        var y = 5;

        var action = () => Assert(!(x == y));

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("x == y");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }

    [Test]
    public void Should_show_clean_expression_text_for_AND_operator()
    {
        var a = true;
        var b = false;

        var action = () => Assert(a && b);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("a && b");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }

    [Test]
    public void Should_show_clean_expression_text_for_OR_operator()
    {
        var x = false;
        var y = false;

        var action = () => Assert(x || y);

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("x || y");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }

    [Test]
    public void Should_show_clean_expression_text_for_nested_logical_operators()
    {
        var a = true;
        var b = false;
        var c = false;

        var action = () => Assert(a && (b || c));

        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("a && (b || c)");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().Contain("b || c");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("Convert");
        action.Should().Throw<SharpAssertionException>()
            .Which.Message.Should().NotContain("DisplayClass");
    }

    static bool ThrowException() => throw new InvalidOperationException("This should not be called due to short-circuit evaluation");
}