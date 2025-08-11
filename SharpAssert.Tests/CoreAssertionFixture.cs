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

        var actual = VerifyThrowsException<SharpAssertionException>(() =>
            AssertThrows<NullReferenceException>(() => throw exception))!;

        actual.Message.Should().Contain(
            "Expected exception of type 'System.NullReferenceException', " +
            "but got 'System.ArgumentException: Value does not fall within the expected range.'");
    }

    [Test]
    public void Assert_throws_unexpected_exception_type_with_custom_message()
    {
        var exception = new ArgumentException("Invalid argument");

        var actual = VerifyThrowsException<SharpAssertionException>(() =>
            AssertThrows<NullReferenceException>(() => throw exception, "Custom message"))!;

        actual.Message.Should().Contain(
            "Custom message\nExpected exception of type 'System.NullReferenceException', " +
            "but got 'System.ArgumentException: Invalid argument'");
    }

    [Test]
    public void Assert_throws_when_no_exception_is_thrown()
    {
        var actual = VerifyThrowsException<SharpAssertionException>(() =>
            AssertThrows<ArgumentException>(() => { /* do nothing */ }))!;

        actual.Message.Should().Contain(
            "Expected exception of type 'System.ArgumentException', but no exception was thrown");
    }

    [Test]
    public void Assert_throws_when_no_exception_is_thrown_with_custom_message()
    {
        var actual = VerifyThrowsException<SharpAssertionException>(() =>
            AssertThrows<ArgumentException>(() => { /* do nothing */ }, "Custom message"))!;

        actual.Message.Should().Contain(
            "Custom message\nExpected exception of type 'System.ArgumentException', but no exception was thrown");
    }

    [Test]
    public void Assert_throws_includes_full_stack_trace_in_error_message()
    {
        var actual = VerifyThrowsException<SharpAssertionException>(() =>
            AssertThrows<NullReferenceException>(() => ThrowDeepException()))!;

        actual.Message.Should().Contain("Full exception details:");
        actual.Message.Should().Contain("at SharpAssert.CoreAssertionFixture.ThrowDeepException()");
        actual.Message.Should().Contain(
            "at SharpAssert.CoreAssertionFixture.<Assert_throws_includes_full_stack_trace_in_error_message>");
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
    public async Task AssertThrowsAsync_catches_expected_exception_type()
    {
        var exception = new InvalidOperationException("Test exception");

        var actual = await AssertThrowsAsync<InvalidOperationException>(async () => 
        {
            await Task.Delay(1);
            throw exception;
        });

        actual.Should().Be(exception);
    }

    [Test]
    public async Task AssertThrowsAsync_returns_caught_exception()
    {
        var actual = await AssertThrowsAsync<ArgumentException>(async () =>
        {
            await Task.Delay(1);
            throw new ArgumentException("Test message");
        });

        actual.Message.Should().Be("Test message");
        actual.Should().BeOfType<ArgumentException>();
    }

    [Test]
    public async Task AssertThrowsAsync_fails_when_no_exception_thrown()
    {
        var actual = await VerifyThrowsException<SharpAssertionException>(async () =>
            await AssertThrowsAsync<ArgumentException>(async () => 
            {
                await Task.Delay(1);
                // No exception thrown
            }));

        actual!.Message.Should().Contain(
            "Expected exception of type 'System.ArgumentException', but no exception was thrown");
    }

    [Test]
    public async Task AssertThrowsAsync_fails_when_wrong_exception_type()
    {
        var actual = await VerifyThrowsException<SharpAssertionException>(async () =>
            await AssertThrowsAsync<ArgumentException>(async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException("Wrong exception");
            }));

        actual!.Message.Should().Contain(
            "Expected exception of type 'System.ArgumentException', " +
            "but got 'System.InvalidOperationException: Wrong exception'");
    }

    [Test]
    public async Task AssertThrowsAsync_includes_custom_message_in_failures()
    {
        var actual = await VerifyThrowsException<SharpAssertionException>(async () =>
            await AssertThrowsAsync<ArgumentException>(async () =>
            {
                await Task.Delay(1);
                // No exception thrown
            }, "Custom failure message"));

        actual!.Message.Should().Contain(
            "Custom failure message\nExpected exception of type 'System.ArgumentException', but no exception was thrown");
    }

    [Test]
    public async Task AssertThrowsAsync_handles_task_cancellation()
    {
        var actual = await AssertThrowsAsync<TaskCanceledException>(async () =>
        {
            var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            await Task.Delay(1000, cts.Token);
        });

        actual.Should().BeOfType<TaskCanceledException>();
    }

    [Test]
    public async Task AssertThrowsAsync_unwraps_aggregate_exceptions()
    {
        var actual = await AssertThrowsAsync<InvalidOperationException>(async () =>
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

        actual.Message.Should().Be("Inner exception");
    }

    [Test]
    public async Task AssertThrowsAsync_includes_full_stack_trace()
    {
        var actual = await VerifyThrowsException<SharpAssertionException>(async () =>
            await AssertThrowsAsync<ArgumentException>(async () => await ThrowDeepAsyncException()));

        actual!.Message.Should().Contain("Full exception details:");
        actual.Message.Should().Contain("at SharpAssert.CoreAssertionFixture.ThrowDeepAsyncException()");
        actual.Message.Should().Contain("InvalidOperationException: Deep async exception");
    }

    [Test]
    public async Task AssertThrowsAsync_works_with_task_returning_functions()
    {
        var actual = await AssertThrowsAsync<ArgumentException>(async () =>
        {
            var result = await Task.FromResult("test");
            throw new ArgumentException($"Processing {result} failed");
        });

        actual.Message.Should().Be("Processing test failed");
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