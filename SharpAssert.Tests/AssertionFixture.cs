using FluentAssertions;
using SharpAssert;
using static Sharp;

namespace SharpAssert.Tests;

[TestFixture]
public class AssertionFixture
{
    [Test]
    public void Should_pass_when_condition_is_true()
    {
        // Act & Assert - should not throw
        var action = () => Assert(true);
        action.Should().NotThrow();
    }

    [Test]
    public void Should_throw_SharpAssertionException_when_false()
    {
        // Act & Assert - should throw SharpAssertionException
        var action = () => Assert(false);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Assertion failed*");
    }

    [Test]
    public void Should_include_expression_text_in_error()
    {
        // Act & Assert - should include expression text
        var action = () => Assert(1 == 2);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*1 == 2*");
    }

    [Test]
    public void Should_include_file_and_line_in_error()
    {
        // Act & Assert - should include file path and line number
        var action = () => Assert(false);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*AssertionFixture.cs:*");
    }
}