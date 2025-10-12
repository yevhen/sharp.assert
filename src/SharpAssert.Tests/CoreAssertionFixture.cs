using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class CoreAssertionFixture : TestBase
{
    [Test]
    public void Should_pass_when_condition_is_true()
    {
        AssertDoesNotThrow(() => Assert(true));
    }

    [Test]
    public void Should_throw_SharpAssertionException_when_false()
    {
        AssertThrows(() => Assert(false), "Assertion failed*");
    }

    [Test]
    public void Should_include_expression_text_in_error()
    {
        AssertThrows(() => Assert(1 == 2), "*1 == 2*");
    }

    [Test]
    public void Should_include_file_and_line_in_error()
    {
        AssertThrows(() => Assert(false), "*AssertionFixture.cs:*");
    }

    [Test]
    public void Should_pass_when_condition_is_true_with_message()
    {
        AssertDoesNotThrow(() => Assert(true, "This should pass"));
    }

    [Test]
    public void Should_include_custom_message_in_error()
    {
        AssertThrows(() => Assert(false, "Custom error message"), "Custom error message*");
    }

    [Test]
    public void Should_include_both_message_and_expression_in_error()
    {
        AssertThrows(() => Assert(1 == 2, "Values should be equal"), "Values should be equal*1 == 2*");
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
    public void Throws_captures_expected_exception()
    {
        var exception = new NullReferenceException();

        var result = Throws<NullReferenceException>(() => throw exception);
        AssertDoesNotThrow(() => Assert(result));

        result.Exception.Should().Be(exception);
    }

    [Test]
    public void Throws_fails_when_unexpected_exception_type()
    {
        var exception = new ArgumentException();

        var actual = VerifyThrowsException<SharpAssertionException>(() => {
            Throws<NullReferenceException>(() => throw exception);
        })!;

        actual.Message.Should().Contain(
            "Expected exception of type 'System.NullReferenceException', " +
            "but got 'System.ArgumentException': Value does not fall within the expected range.");
    }

    [Test]
    public void Throws_fails_when_unexpected_exception_type_with_detailed_message()
    {
        var exception = new ArgumentException("Invalid argument");

        var actual = VerifyThrowsException<SharpAssertionException>(() =>
            Throws<NullReferenceException>(() => throw exception))!;

        actual.Message.Should().Contain(
            "Expected exception of type 'System.NullReferenceException', " +
            "but got 'System.ArgumentException': Invalid argument");

        actual.Message.Should().Contain("Full exception details:");
    }

    [Test]
    public void Assert_does_not_throw_when_no_exception_is_thrown()
    {
        var result = Throws<ArgumentException>(() => { /* do nothing */ });
        AssertDoesNotThrow(() => Assert(!result));
    }

    [Test]
    public void Can_check_exception_message_directly()
    {
        var result = Throws<ArgumentException>(() =>
                throw new ArgumentException("Invalid parameter"));
        AssertDoesNotThrow(() => Assert(result.Message == "Invalid parameter"));
    }

    [Test]
    public void Throws_includes_full_stack_trace_in_error_message()
    {
        var actual = VerifyThrowsException<SharpAssertionException>(() =>
            Throws<NullReferenceException>(() => ThrowDeepException()))!;

        actual.Message.Should().Contain("Full exception details:");
        actual.Message.Should().Contain("at SharpAssert.CoreAssertionFixture.ThrowDeepException()");
        actual.Message.Should().Contain(
            "at SharpAssert.CoreAssertionFixture.<Throws_includes_full_stack_trace_in_error_message>");
    }

    void ThrowDeepException() => throw new InvalidOperationException("Deep exception with stack trace");

    static T? VerifyThrowsException<T>(Action action) where T : Exception
    {
        try
        {
            action();
            return null;
        }
        catch (T ex)
        {
            return ex;
        }
    }

    [Test]
    public async Task ThrowsAsync_catches_expected_exception_type()
    {
        var exception = new InvalidOperationException("Test exception");

        var result = await ThrowsAsync<InvalidOperationException>(async () =>
        {
            await Task.Delay(1);
            throw exception;
        });

        AssertDoesNotThrow(() => Assert(result));
        result.Exception.Should().Be(exception);
    }

    [Test]
    public async Task ThrowsAsync_can_check_exception_message()
    {
        var result = await ThrowsAsync<ArgumentException>(async () =>
        {
            await Task.Delay(1);
            throw new ArgumentException("Test message");
        });

        AssertDoesNotThrow(() => Assert(result && result.Message == "Test message"));
        result.Exception.Should().BeOfType<ArgumentException>();
    }

    [Test]
    public async Task ThrowsAsync_detects_when_no_exception_thrown()
    {
        var result = await ThrowsAsync<ArgumentException>(async () =>
        {
            await Task.Delay(1);
            // No exception thrown
        });

        AssertDoesNotThrow(() => Assert(!result));
    }

    [Test]
    public async Task ThrowsAsync_fails_when_wrong_exception_type()
    {
        var actual = await VerifyThrowsException<SharpAssertionException>(async () =>
            await ThrowsAsync<ArgumentException>(async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Wrong exception");
            }));

        actual!.Message.Should().Contain(
            "Expected exception of type 'System.ArgumentException', " +
            "but got 'System.InvalidOperationException': Wrong exception");
    }

    [Test]
    public async Task ThrowsAsync_fails_when_no_exception_thrown()
    {
        var result = await ThrowsAsync<ArgumentException>(async () =>
        {
            await Task.Delay(1);
            // No exception thrown
        });

        var actual = VerifyThrowsException<InvalidOperationException>(() => _ = result.Exception);

        actual!.Message.Should().Contain(
            "Expected exception of type 'System.ArgumentException', but no exception was thrown");
    }

    [Test]
    public async Task ThrowsAsync_unwraps_aggregate_exceptions()
    {
        var result = await ThrowsAsync<InvalidOperationException>(async () =>
        {
            var task = Task.Run(() => throw new InvalidOperationException("Inner exception"));
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                // Simulate AggregateException wrapping
                throw new AggregateException(ex);
            }
        });

        AssertDoesNotThrow(() => Assert(result && result.Message == "Inner exception"));
    }

    [Test]
    public async Task ThrowsAsync_includes_full_stack_trace()
    {
        var actual = await VerifyThrowsException<SharpAssertionException>(async () =>
            await ThrowsAsync<ArgumentException>(async () => await ThrowDeepAsyncException()));

        actual!.Message.Should().Contain("Full exception details:");
        actual.Message.Should().Contain("at SharpAssert.CoreAssertionFixture.ThrowDeepAsyncException()");
        actual.Message.Should().Contain("InvalidOperationException: Deep async exception");
    }

    static async Task ThrowDeepAsyncException()
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Deep async exception with stack trace");
    }

    static async Task<T?> VerifyThrowsException<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();
            return null;
        }
        catch (T ex)
        {
            return ex;
        }
    }
}