using static SharpAssert.Sharp;

namespace SharpAssert;

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

    static bool ThrowException() => throw new InvalidOperationException("This should not be called due to short-circuit evaluation");
}