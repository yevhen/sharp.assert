using FluentAssertions;
using static Sharp;

namespace SharpAssert;

[TestFixture]
public class CoreAssertionFixture : TestBase
{
    [Test]
    public void Should_pass_when_condition_is_true()
    {
        var action = () => Assert(true);
        action.Should().NotThrow();
    }

    [Test]
    public void Should_throw_SharpAssertionException_when_false()
    {
        var action = () => Assert(false);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Assertion failed*");
    }

    [Test]
    public void Should_include_expression_text_in_error()
    {
        var action = () => Assert(1 == 2);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*1 == 2*");
    }

    [Test]
    public void Should_include_file_and_line_in_error()
    {
        var action = () => Assert(false);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*AssertionFixture.cs:*");
    }

    [Test]
    public void Should_pass_when_condition_is_true_with_message()
    {
        var action = () => Assert(true, "This should pass");
        action.Should().NotThrow();
    }

    [Test]
    public void Should_include_custom_message_in_error()
    {
        var action = () => Assert(false, "Custom error message");
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Custom error message*");
    }

    [Test]
    public void Should_include_both_message_and_expression_in_error()
    {
        var action = () => Assert(1 == 2, "Values should be equal");
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Values should be equal*1 == 2*");
    }

    [Test]
    public void Should_reject_empty_message()
    {
        var action = () => Assert(true, "");
        action.Should().Throw<ArgumentException>()
              .WithMessage("*Message must be either null or non-empty*");
    }

    [Test]
    public void Should_reject_whitespace_message()
    {
        var action = () => Assert(true, "   ");
        action.Should().Throw<ArgumentException>()
              .WithMessage("*Message must be either null or non-empty*");
    }
}