using System.Linq.Expressions;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
// NOTE: This fixture intentionally tests internal API directly.
// Most tests should use Sharp.Assert() public API instead.
public class SharpInternalAssertionFixture : TestBase
{
    [Test]
    public void Should_pass_when_expression_evaluates_to_true()
    {
        Expression<Func<bool>> expr = () => 1 == 1;
        var action = () => SharpInternal.Assert(expr, "1 == 1", "TestFile.cs", 10);
        action.Should().NotThrow();
    }

    [Test]
    public void Should_pass_when_expression_evaluates_to_true_with_message()
    {
        Expression<Func<bool>> expr = () => 1 == 1;
        var action = () => SharpInternal.Assert(expr, "1 == 1", "TestFile.cs", 10, "Should be equal");
        action.Should().NotThrow();
    }

    [Test]
    public void Should_throw_with_message_when_expression_fails()
    {
        Expression<Func<bool>> expr = () => 1 == 2;
        var action = () => SharpInternal.Assert(expr, "1 == 2", "TestFile.cs", 10, "Numbers should match");
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Numbers should match*");
    }

    [Test]
    public void Should_include_both_message_and_detailed_analysis()
    {
        var x = 1;
        var y = 2;
        Expression<Func<bool>> expr = () => x == y;
        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 10, "Custom error");
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("Custom error*Left:  1*Right: 2*");
    }

    [Test]
    public void Should_reject_empty_message()
    {
        Expression<Func<bool>> expr = () => true;
        var action = () => SharpInternal.Assert(expr, "true", "TestFile.cs", 10, "");
        action.Should().Throw<ArgumentException>()
              .WithMessage("*Message must be either null or non-empty*");
    }

    [Test]
    public void Should_reject_whitespace_message()
    {
        Expression<Func<bool>> expr = () => true;
        var action = () => SharpInternal.Assert(expr, "true", "TestFile.cs", 10, "   ");
        action.Should().Throw<ArgumentException>()
              .WithMessage("*Message must be either null or non-empty*");
    }

    [Test]
    public void Should_pass_when_complex_logical_expression_succeeds()
    {
        var x = 5;
        var y = 10;
        var z = 15;
        AssertExpressionPasses(() => x < y && y < z && (x + y) == z);
    }

    [Test]
    public void Should_pass_when_method_chain_assertion_succeeds()
    {
        var text = "Hello World";
        AssertExpressionPasses(() => text.ToLower().Contains("hello") && text.Length > 5);
    }

    [Test]
    public void Should_pass_when_nested_expression_succeeds()
    {
        var items = new List<string> { "apple", "banana", "cherry" };
        AssertExpressionPasses(() => items.Where(x => x.StartsWith("a")).Count() == 1);
    }

    [Test]
    public void Should_pass_when_mixed_type_expression_succeeds()
    {
        var number = 42;
        var text = "42";
        AssertExpressionPasses(() => number.ToString() == text && int.Parse(text) == number);
    }

}