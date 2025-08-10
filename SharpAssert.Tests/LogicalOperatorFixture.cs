using System.Linq.Expressions;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class LogicalOperatorFixture : TestBase
{
    [Test]
    public void Should_show_which_part_of_AND_failed()
    {
        var left = true;
        var right = false;
        Expression<Func<bool>> expr = () => left && right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left && right", "TestFile.cs", 10, "*&&*true*false*");
    }

    [Test]
    public void Should_short_circuit_AND_correctly()
    {
        var left = false;
        Expression<Func<bool>> expr = () => left && ThrowException();

        AssertExpressionThrows<SharpAssertionException>(expr, "left && ThrowException()", "TestFile.cs", 20, "*&&*false*");
    }

    [Test]
    public void Should_show_which_part_of_OR_failed()
    {
        var left = false;
        var right = false;
        Expression<Func<bool>> expr = () => left || right;

        AssertExpressionThrows<SharpAssertionException>(expr, "left || right", "TestFile.cs", 30, "*||*false*false*");
    }

    [Test]
    public void Should_pass_when_OR_succeeds()
    {
        var left = false;
        var right = true;
        Expression<Func<bool>> expr = () => left || right;

        SharpInternal.Assert(expr, "left || right", "TestFile.cs", 35);
    }

    [Test]
    public void Should_pass_when_AND_succeeds()
    {
        var left = true;
        var right = true;
        Expression<Func<bool>> expr = () => left && right;

        var action = () => SharpInternal.Assert(expr, "left && right", "TestFile.cs", 45);
        action.Should().NotThrow("both operands are true");
    }

    [Test]
    public void Should_pass_when_NOT_succeeds()
    {
        var operand = false;
        Expression<Func<bool>> expr = () => !operand;

        var action = () => SharpInternal.Assert(expr, "!operand", "TestFile.cs", 50);
        action.Should().NotThrow("NOT false should be true");
    }

    [Test]
    public void Should_handle_NOT_operator()
    {
        var operand = true;
        Expression<Func<bool>> expr = () => !operand;

        AssertExpressionThrows<SharpAssertionException>(expr, "!operand", "TestFile.cs", 40, "*!*true*");
    }

    static bool ThrowException() => throw new InvalidOperationException("This should not be called due to short-circuit evaluation");
}