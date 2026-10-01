using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class InlineExceptionChecksFixture
{
    [Test]
    public void Should_report_missing_exception_at_the_assertion()
    {
        var calls = 0;
        Action operation = () => calls++;

        Action assertion = () => Assert(Throws<ArgumentException>(operation).Message == "expected", "Message check");

        var error = assertion.Should().Throw<SharpAssertionException>().Which;
        error.Result!.Context.Expression.Should().Contain("Throws<ArgumentException>(operation).Message");
        error.Result.Context.Message.Should().Be("Message check");
        error.Message.Should().Contain("System.ArgumentException").And.Contain("no exception was thrown")
            .And.Contain("InlineExceptionChecksFixture.cs:");
        calls.Should().Be(1);
    }

    [Test]
    public void Should_report_wrong_exception_type_at_the_assertion()
    {
        var calls = 0;
        Action operation = () => { calls++; throw new InvalidOperationException("wrong type"); };

        Action assertion = () => Assert(Throws<ArgumentException>(operation).Message == "expected");

        var error = assertion.Should().Throw<SharpAssertionException>().Which;
        error.Result.Should().NotBeNull();
        error.Message.Should().Contain("System.ArgumentException").And.Contain("System.InvalidOperationException")
            .And.Contain("wrong type");
        calls.Should().Be(1);
    }

    [Test]
    public void Should_keep_message_comparison_diagnostics_without_repeating_the_action()
    {
        var calls = 0;
        Action operation = () => { calls++; throw new ArgumentException("actual"); };

        Action assertion = () => Assert(Throws<ArgumentException>(operation).Message == "expected");

        assertion.Should().Throw<SharpAssertionException>().Which.Message.Should().Contain("actual").And.Contain("expected");
        calls.Should().Be(1);
    }

    [Test]
    public void Should_pass_matching_inline_message_once()
    {
        var calls = 0;
        Action operation = () => { calls++; throw new ArgumentException("expected"); };

        Assert(Throws<ArgumentException>(operation).Message == "expected");

        calls.Should().Be(1);
    }

    [Test]
    public void Should_report_missing_exception_for_inline_method_checks()
    {
        Action assertion = () => Assert(Throws<ArgumentException>(() => { }).Message.Contains("expected"));

        assertion.Should().Throw<SharpAssertionException>().Which.Message.Should().Contain("no exception was thrown");
    }

    [Test]
    public void Should_report_missing_exception_for_captured_results()
    {
        var result = Throws<ArgumentException>(() => { });

        Action assertion = () => Assert(result.Exception.ParamName == "name");

        assertion.Should().Throw<SharpAssertionException>().Which.Message.Should().Contain("no exception was thrown");
    }

    [Test]
    public void Should_report_missing_exception_for_data_checks()
    {
        Action assertion = () => Assert(Throws<ArgumentException>(() => { }).Data.Contains("key"));

        assertion.Should().Throw<SharpAssertionException>().Which.Message.Should().Contain("no exception was thrown");
    }

    [Test]
    public void Should_report_missing_exception_for_implicit_exception_conversions()
    {
        Action assertion = () => Assert(((Exception)Throws<ArgumentException>(() => { })).Message == "expected");

        assertion.Should().Throw<SharpAssertionException>().Which.Message.Should().Contain("no exception was thrown");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_report_inline_async_exception_failures_once(bool wrongType)
    {
        var calls = 0;
        Func<Task> operation = () =>
        {
            calls++;
            return wrongType ? Task.FromException(new InvalidOperationException("wrong type")) : Task.CompletedTask;
        };
        Func<Task> assertion = async () => Assert((await ThrowsAsync<ArgumentException>(operation)).Message == "expected");

        var error = (await assertion.Should().ThrowAsync<SharpAssertionException>()).Which;
        error.Result.Should().NotBeNull();
        error.Message.Should().Contain("System.ArgumentException");
        calls.Should().Be(1);
    }

    [Test]
    public async Task Should_pass_inline_async_exception_message_once()
    {
        var calls = 0;
        Func<Task> operation = () => { calls++; return Task.FromException(new ArgumentException("expected")); };

        Assert((await ThrowsAsync<ArgumentException>(operation)).Message == "expected");

        calls.Should().Be(1);
    }

    [Test]
    public void Should_keep_action_factory_out_variables_in_the_original_scope()
    {
        var source = """
            using System;
            using static SharpAssert.Sharp;
            public class Sample
            {
                static Action Operation(out int marker)
                {
                    marker = 42;
                    return () => throw new ArgumentException("expected");
                }
                public static void Check()
                {
                    Assert(Throws<ArgumentException>(Operation(out var marker)).Message == "expected");
                    Assert(marker == 42);
                }
            }
            """;

        RewrittenSource.LoadCheck(source)();
    }

    [Test]
    public void Should_not_change_exception_checks_inside_the_user_action()
    {
        Assert(Throws<ArgumentException>(() =>
        {
            Assert(!Throws<InvalidOperationException>(() => { }));
            throw new ArgumentException("expected");
        }).Message == "expected");
    }

    [Test]
    public void Should_not_guard_exception_result_type_names()
    {
        var name = typeof(ExceptionResult<ArgumentException>).Name;

        Assert(Throws<ArgumentException>(() => throw new ArgumentException(name)).Message == typeof(ExceptionResult<ArgumentException>).Name);
    }

    [Test]
    public void Should_keep_negated_Throws_behavior()
    {
        var calls = 0;

        Assert(!Throws<ArgumentException>(() => calls++));

        calls.Should().Be(1);
    }

    [Test]
    public void Should_not_catch_unrelated_invalid_operation_exceptions()
    {
        var original = new InvalidOperationException("user getter");
        Func<string> expected = () => throw original;

        Action assertion = () => Assert(Throws<ArgumentException>(() => throw new ArgumentException("actual")).Message == expected());

        assertion.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(original);
    }
}
