using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class CompoundFailureMessageFixture
{
    [TestCase(0, 2, 3, 4, "a == 1", 1)]
    [TestCase(1, 0, 3, 4, "b == 2", 2)]
    [TestCase(1, 2, 0, 4, "c == 3", 3)]
    [TestCase(1, 2, 3, 0, "d == 4", 4)]
    public void Should_show_only_the_failed_AND_condition(int a, int b, int c, int d, string failed, int expected)
    {
        Action assertion = () => Assert(a == 1 && b == 2 && c == 3 && d == 4);

        ShouldRender(assertion, $"  {failed}\n    Left:  0\n    Right: {expected}");
    }

    [Test]
    public void Should_name_failed_boolean_conditions()
    {
        var first = true;
        var second = false;
        var third = true;
        Action assertion = () => Assert(first && second && third);

        ShouldRender(assertion, "  second: False");
    }

    [Test]
    public void Should_explain_both_failed_OR_alternatives()
    {
        var left = false;
        var right = false;
        Action assertion = () => Assert(left || right);

        ShouldRender(assertion, "  No alternative was true:\n    left: False\n    right: False");
    }

    [Test]
    public void Should_keep_mixed_operator_groups()
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        Action assertion = () => Assert((a == 0 && b == 0) || (c == 3 && d == 0));

        ShouldRender(assertion, "  No alternative was true:\n    a == 0\n      Left:  1\n      Right: 0\n    d == 0\n      Left:  4\n      Right: 0");
    }

    [Test]
    public void Should_explain_a_negated_OR_without_evaluating_the_other_branch()
    {
        var left = true;
        Func<bool> right = () => throw new InvalidOperationException("Not reached");
        Action assertion = () => Assert(!(left || right()));

        ShouldRender(assertion, "  (left || right()) was True\n    left: True");
    }

    [Test]
    public void Should_explain_a_negated_AND()
    {
        var left = true;
        var right = true;
        Action assertion = () => Assert(!(left && right));

        ShouldRender(assertion, "  (left && right) was True\n    All conditions were true:\n      left: True\n      right: True");
    }

    static void ShouldRender(Action assertion, string details)
    {
        var error = assertion.Should().Throw<SharpAssertionException>().Which;

        error.Message.Should().Be(error.Result!.Context.FormatMessage() + details.Replace("\n", Environment.NewLine));
        error.Result.ToString().Should().Be("Failed");
    }
}
