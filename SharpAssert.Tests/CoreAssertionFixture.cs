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
}