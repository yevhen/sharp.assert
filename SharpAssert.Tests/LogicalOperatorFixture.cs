using FluentAssertions;
using SharpAssert;
using System.Linq.Expressions;

namespace SharpAssert.Tests;

[TestFixture]
public class LogicalOperatorFixture
{
    [Test]
    public void Should_show_which_part_of_AND_failed()
    {
        bool left = true;
        bool right = false;
        Expression<Func<bool>> expr = () => left && right;

        var action = () => SharpInternal.Assert(expr, "left && right", "TestFile.cs", 10);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*&&*true*false*");
    }

    [Test]
    public void Should_short_circuit_AND_correctly()
    {
        bool left = false;
        Expression<Func<bool>> expr = () => left && ThrowException();

        var action = () => SharpInternal.Assert(expr, "left && ThrowException()", "TestFile.cs", 20);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*&&*false*");
    }

    [Test]
    public void Should_show_which_part_of_OR_failed()
    {
        bool left = false;
        bool right = false;
        Expression<Func<bool>> expr = () => left || right;

        var action = () => SharpInternal.Assert(expr, "left || right", "TestFile.cs", 30);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*||*false*false*");
    }

    [Test]
    public void Should_pass_when_OR_succeeds()
    {
        bool left = false;
        bool right = true;
        Expression<Func<bool>> expr = () => left || right;

        SharpInternal.Assert(expr, "left || right", "TestFile.cs", 35);
    }

    [Test]
    public void Should_handle_NOT_operator()
    {
        bool operand = true;
        Expression<Func<bool>> expr = () => !operand;

        var action = () => SharpInternal.Assert(expr, "!operand", "TestFile.cs", 40);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*!*true*");
    }

    private static bool ThrowException()
    {
        throw new InvalidOperationException("This should not be called due to short-circuit evaluation");
    }
}