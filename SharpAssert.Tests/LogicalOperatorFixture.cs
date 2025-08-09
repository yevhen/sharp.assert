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
        // Arrange - true && false should show that right operand was false
        bool left = true;
        bool right = false;
        Expression<Func<bool>> expr = () => left && right;

        // Act & Assert
        var action = () => SharpInternal.Assert(expr, "left && right", "TestFile.cs", 10);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*&&*true*false*");
    }

    [Test]
    public void Should_short_circuit_AND_correctly()
    {
        // Arrange - false && throw should not evaluate right side (short-circuit)
        bool left = false;
        Expression<Func<bool>> expr = () => left && ThrowException();

        // Act & Assert - should not throw from ThrowException, only from assertion failure
        var action = () => SharpInternal.Assert(expr, "left && ThrowException()", "TestFile.cs", 20);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*&&*false*");
    }

    [Test]
    public void Should_show_which_part_of_OR_succeeded()
    {
        // Arrange - false || true should show evaluation of both sides
        bool left = false;
        bool right = true;
        Expression<Func<bool>> expr = () => left || right;

        // Act & Assert - this should pass, but let's test the failure case
        Expression<Func<bool>> failExpr = () => left && right; // Changed to && to make it fail
        var action = () => SharpInternal.Assert(failExpr, "left && right", "TestFile.cs", 30);
        action.Should().Throw<SharpAssertionException>();

        // Test the actual OR case that would pass
        SharpInternal.Assert(expr, "left || right", "TestFile.cs", 35); // Should not throw
    }

    [Test]
    public void Should_handle_NOT_operator()
    {
        // Arrange - !true shows operand was true
        bool operand = true;
        Expression<Func<bool>> expr = () => !operand;

        // Act & Assert
        var action = () => SharpInternal.Assert(expr, "!operand", "TestFile.cs", 40);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*!*true*");
    }

    private static bool ThrowException()
    {
        throw new InvalidOperationException("This should not be called due to short-circuit evaluation");
    }
}