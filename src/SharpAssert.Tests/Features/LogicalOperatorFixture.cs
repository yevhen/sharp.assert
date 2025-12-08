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

    [Test]
    public void Should_handle_deeply_nested_parentheses_passing()
    {
        var a = 5;
        var b = 5;

        AssertDoesNotThrow(() => Assert((((a == b)))));
    }

    [Test]
    public void Should_handle_deeply_nested_parentheses_failing()
    {
        var a = 5;
        var b = 10;

        AssertThrows(() => Assert((((a == b)))), "*== *Left:  5*Right: 10*");
    }

    [Test]
    public void Should_handle_simple_negation_passing()
    {
        var a = false;

        AssertDoesNotThrow(() => Assert(!a));
    }

    [Test]
    public void Should_handle_simple_negation_failing()
    {
        var a = true;

        AssertThrows(() => Assert(!a), "*!*true*");
    }

    [Test]
    public void Should_handle_mixed_logical_operators_passing()
    {
        var a = false;
        var b = true;
        var c = false;
        var d = true;

        AssertDoesNotThrow(() => Assert(!a && (b || c) && d));
    }

    [Test]
    public void Should_handle_mixed_logical_operators_failing_on_NOT()
    {
        var a = true;
        var b = true;
        var c = false;
        var d = true;

        AssertThrows(() => Assert(!a && (b || c) && d), "*!*true*&&*false*");
    }

    [Test]
    public void Should_handle_mixed_logical_operators_failing_on_OR()
    {
        var a = false;
        var b = false;
        var c = false;
        var d = true;

        AssertThrows(() => Assert(!a && (b || c) && d), "*||*false*false*&&*false*");
    }

    [Test]
    public void Should_handle_mixed_logical_operators_failing_on_right_AND()
    {
        var a = false;
        var b = true;
        var c = false;
        var d = false;

        AssertThrows(() => Assert(!a && (b || c) && d), "*&&*false*");
    }

    [Test]
    public void Should_handle_method_calls_in_logical_expressions_passing()
    {
        var list = new List<int> { 1, 2, 3 };
        var x = 2;
        var y = 5;

        AssertDoesNotThrow(() => Assert(list.Contains(x) && y > 0));
    }

    [Test]
    public void Should_handle_method_calls_in_logical_expressions_failing_on_method()
    {
        var list = new List<int> { 1, 2, 3 };
        var x = 5;
        var y = 10;

        AssertThrows(() => Assert(list.Contains(x) && y > 0), "*&&*false*");
    }

    [Test]
    public void Should_handle_method_calls_in_logical_expressions_failing_on_comparison()
    {
        var list = new List<int> { 1, 2, 3 };
        var x = 2;
        var y = -5;

        AssertThrows(() => Assert(list.Contains(x) && y > 0), "*>*-5*0*&&*false*");
    }

    [Test]
    public void Should_recurse_through_nested_parenthesized_logical_expressions_passing()
    {
        var a = true;
        var b = true;
        var c = true;
        var d = true;

        AssertDoesNotThrow(() => Assert(((a && b)) || ((c && d))));
    }

    [Test]
    public void Should_recurse_through_nested_parenthesized_logical_expressions_failing()
    {
        var a = true;
        var b = false;
        var c = true;
        var d = false;

        AssertThrows(() => Assert(((a && b)) || ((c && d))),
            "*||*" +
            "*Left: ((a && b))*" +
            "*Left: True*" +
            "*Right: False*" +
            "*&&: Right operand was false*" +
            "*Right: ((c && d))*" +
            "*Left: True*" +
            "*Right: False*" +
            "*&&: Right operand was false*" +
            "*||: Both operands were false*");
    }

    [Test]
    public void Should_recurse_through_triple_nested_parenthesized_AND_passing()
    {
        var a = true;
        var b = true;

        AssertDoesNotThrow(() => Assert((((a))) && (((b)))));
    }

    [Test]
    public void Should_recurse_through_triple_nested_parenthesized_AND_failing()
    {
        var a = true;
        var b = false;

        AssertThrows(() => Assert((((a))) && (((b)))),
            "*&&*" +
            "*Left: True*" +
            "*Right: False*" +
            "*&&: Right operand was false*");
    }

    static bool ThrowException() => throw new InvalidOperationException("This should not be called due to short-circuit evaluation");
}