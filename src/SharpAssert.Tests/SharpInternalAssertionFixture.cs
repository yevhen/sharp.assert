using static SharpAssert.Sharp;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class SharpInternalAssertionFixture : TestBase
{
    [Test]
    public void Should_pass_when_expression_evaluates_to_true()
    {
        AssertPasses(() => Assert(1 == 1));
    }

    [Test]
    public void Should_pass_when_expression_evaluates_to_true_with_message()
    {
        AssertPasses(() => Assert(1 == 1, "Should be equal"));
    }

    [Test]
    public void Should_throw_with_message_when_expression_fails()
    {
        var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(1 == 2, "Numbers should match"));
        exception.Result!.Context.Message.Should().Be("Numbers should match");
    }

    [Test]
    public void Should_include_both_message_and_detailed_analysis()
    {
        var x = 1;
        var y = 2;
        var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(x == y, "Custom error"));
        
        exception.Result!.Context.Message.Should().Be("Custom error");
        exception.Result.Result.Should().BeEquivalentTo(
            BinaryComparison("x == y", Equal, Comparison(1, 2)), 
            options => options.RespectingRuntimeTypes());
    }

    [Test]
    public void Should_reject_empty_message()
    {
        NUnit.Framework.Assert.Throws<ArgumentException>(() => Assert(true, ""));
    }

    [Test]
    public void Should_reject_whitespace_message()
    {
        NUnit.Framework.Assert.Throws<ArgumentException>(() => Assert(true, "   "));
    }

    [Test]
    public void Should_pass_when_complex_logical_expression_succeeds()
    {
        var x = 5;
        var y = 10;
        var z = 15;
        AssertPasses(() => Assert(x < y && y < z && (x + y) == z));
    }

    [Test]
    public void Should_pass_when_method_chain_assertion_succeeds()
    {
        var text = "Hello World";
        AssertPasses(() => Assert(text.ToLower().Contains("hello") && text.Length > 5));
    }

    [Test]
    public void Should_pass_when_nested_expression_succeeds()
    {
        var items = new List<string> { "apple", "banana", "cherry" };
        AssertPasses(() => Assert(items.Where(x => x.StartsWith("a")).Count() == 1));
    }

    [Test]
    public void Should_pass_when_mixed_type_expression_succeeds()
    {
        var number = 42;
        var text = "42";
        AssertPasses(() => Assert(number.ToString() == text && int.Parse(text) == number));
    }

    static Features.BinaryComparison.DefaultComparisonResult Comparison(object? left, object? right) =>
        new(Operand(left), Operand(right));
}
