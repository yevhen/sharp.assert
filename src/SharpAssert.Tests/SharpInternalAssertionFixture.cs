using System.Linq.Expressions;
using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class SharpInternalAssertionFixture : TestBase
{
    [Test]
    public void Should_pass_when_expression_evaluates_to_true()
    {
        AssertDoesNotThrow(() => Assert(1 == 1));
    }

    [Test]
    public void Should_pass_when_expression_evaluates_to_true_with_message()
    {
        AssertDoesNotThrow(() => Assert(1 == 1, "Should be equal"));
    }

    [Test]
    public void Should_throw_with_message_when_expression_fails()
    {
        AssertThrows(() => Assert(1 == 2, "Numbers should match"), "Numbers should match*");
    }

    [Test]
    public void Should_include_both_message_and_detailed_analysis()
    {
        var x = 1;
        var y = 2;
        AssertThrows(() => Assert(x == y, "Custom error"), "Custom error*Left:  1*Right: 2*");
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

    [Test]
    public void Should_pass_when_complex_logical_expression_succeeds()
    {
        var x = 5;
        var y = 10;
        var z = 15;
        AssertDoesNotThrow(() => Assert(x < y && y < z && (x + y) == z));
    }

    [Test]
    public void Should_pass_when_method_chain_assertion_succeeds()
    {
        var text = "Hello World";
        AssertDoesNotThrow(() => Assert(text.ToLower().Contains("hello") && text.Length > 5));
    }

    [Test]
    public void Should_pass_when_nested_expression_succeeds()
    {
        var items = new List<string> { "apple", "banana", "cherry" };
        AssertDoesNotThrow(() => Assert(items.Where(x => x.StartsWith("a")).Count() == 1));
    }

    [Test]
    public void Should_pass_when_mixed_type_expression_succeeds()
    {
        var number = 42;
        var text = "42";
        AssertDoesNotThrow(() => Assert(number.ToString() == text && int.Parse(text) == number));
    }

}