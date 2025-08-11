using FluentAssertions;
using static NUnit.Framework.Assert;
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

    [Test]
    public void Assert_throws_expected_exception_type()
    {
        var exception = new NullReferenceException();

        var actual = AssertThrows<NullReferenceException>(() => throw exception);

        actual.Should().Be(exception);
    }

    [Test]
    public void Assert_throws_unexpected_exception_type()
    {
        var exception = new ArgumentException();

        var actual = Throws<SharpAssertionException>(() =>
            AssertThrows<NullReferenceException>(() => throw exception))!;

        actual.Message.Should().Contain(
            "Expected exception of type 'System.NullReferenceException', " +
            "but got 'System.ArgumentException: Value does not fall within the expected range.'");
    }

    [Test]
    public void Assert_throws_unexpected_exception_type_with_custom_message()
    {
        var exception = new ArgumentException("Invalid argument");

        var actual = Throws<SharpAssertionException>(() =>
            AssertThrows<NullReferenceException>(() => throw exception, "Custom message"))!;

        actual.Message.Should().Contain(
            "Custom message\nExpected exception of type 'System.NullReferenceException', " +
            "but got 'System.ArgumentException: Invalid argument'");
    }

    [Test]
    public void Assert_throws_when_no_exception_is_thrown()
    {
        var actual = Throws<SharpAssertionException>(() =>
            AssertThrows<ArgumentException>(() => { /* do nothing */ }))!;

        actual.Message.Should().Contain(
            "Expected exception of type 'System.ArgumentException', but no exception was thrown");
    }

    [Test]
    public void Assert_throws_when_no_exception_is_thrown_with_custom_message()
    {
        var actual = Throws<SharpAssertionException>(() =>
            AssertThrows<ArgumentException>(() => { /* do nothing */ }, "Custom message"))!;

        actual.Message.Should().Contain(
            "Custom message\nExpected exception of type 'System.ArgumentException', but no exception was thrown");
    }

    [Test]
    public void Assert_throws_includes_full_stack_trace_in_error_message()
    {
        var actual = Throws<SharpAssertionException>(() =>
            AssertThrows<NullReferenceException>(() => ThrowDeepException()))!;

        actual.Message.Should().Contain("Full exception details:");
        actual.Message.Should().Contain("at SharpAssert.CoreAssertionFixture.ThrowDeepException()");
        actual.Message.Should().Contain(
            "at SharpAssert.CoreAssertionFixture.<Assert_throws_includes_full_stack_trace_in_error_message>");
    }

    void ThrowDeepException()
    {
        throw new InvalidOperationException("Deep exception with stack trace");
    }
}