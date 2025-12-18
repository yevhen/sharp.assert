using static SharpAssert.Sharp;
using FluentAssertions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;

namespace SharpAssert;

[TestFixture]
public class CoreAssertionFixture : TestBase
{
    [TestFixture]
    class AssertionTests
    {
        [Test]
        public void Should_pass_when_true()
        {
            AssertPasses(() => Assert(true));
        }

        [Test]
        public void Should_fail_when_false()
        {
            var expected = Value("false", false, typeof(bool));
            AssertFails(() => Assert(false), expected);
        }

        [Test]
        public void Should_include_expression_text()
        {
            // Compiler optimizes "1 == 2" to constant "False" in expression tree
            var expected = Value("1 == 2", false, typeof(bool));
            AssertFails(() => Assert(1 == 2), expected);
        }

        [Test]
        public void Should_include_file_and_line()
        {
            var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(false));
            exception.Result.Should().NotBeNull();
            exception.Result!.Context.File.Should().EndWith("CoreAssertionFixture.cs");
            exception.Result!.Context.Line.Should().BeGreaterThan(0);
        }

        [Test]
        public void Should_include_custom_message()
        {
            var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(false, "Custom error"));
            exception.Result!.Context.Message.Should().Be("Custom error");
        }

        [Test]
        public void Should_reject_invalid_message()
        {
            NUnit.Framework.Assert.Throws<ArgumentException>(() => Assert(true, ""));
            NUnit.Framework.Assert.Throws<ArgumentException>(() => Assert(true, "   "));
        }
    }

    [TestFixture]
    class ThrowsTests
    {
        [Test]
        public void Throws_should_pass_when_expected_exception_thrown()
        {
            var result = Throws<InvalidOperationException>(() => throw new InvalidOperationException());
            AssertPasses(() => Assert(result));
        }

        [Test]
        public void Throws_should_return_exception_object()
        {
            var ex = new InvalidOperationException("test");
            var result = Throws<InvalidOperationException>(() => throw ex);
            result.Exception.Should().Be(ex);
        }

        [Test]
        public void Negated_Throws_should_include_caught_exception()
        {
            var result = Throws<InvalidOperationException>(() => throw new InvalidOperationException("boom"));

            var expected = new UnaryEvaluationResult(
                "!result",
                UnaryOperator.Not,
                ExpectationResults.Boolean(
                    "result",
                    true,
                    $"Caught: {typeof(InvalidOperationException).FullName}: boom"),
                true,
                false);

            AssertFails(() => Assert(!result), expected);
        }

        [Test]
        public void Throws_OR_should_short_circuit()
        {
            var result = Throws<InvalidOperationException>(() => throw new InvalidOperationException("boom"));

            var right = new ThrowingExpectation();
            AssertPasses(() => Assert(result.Or(right)));
        }

        [Test]
        public void Throws_AND_should_render_both_failures()
        {
            var result = Throws<ArgumentException>(() => { });
            var right = new FailingExpectation("Right failed");

            var expected = new ComposedExpectationEvaluationResult(
                "result.And(right)",
                "AND",
                ExpectationResults.Fail(
                    "result",
                    $"Expected exception of type '{typeof(ArgumentException).FullName}', but no exception was thrown"),
                ExpectationResults.Fail("right", "Right failed"),
                false,
                false);

            AssertFails(() => Assert(result.And(right)), expected);
        }

        [Test]
        public void Throws_OR_should_render_both_failures()
        {
            var result = Throws<ArgumentException>(() => { });
            var right = new FailingExpectation("Right failed");

            var expected = new ComposedExpectationEvaluationResult(
                "result.Or(right)",
                "OR",
                ExpectationResults.Fail(
                    "result",
                    $"Expected exception of type '{typeof(ArgumentException).FullName}', but no exception was thrown"),
                ExpectationResults.Fail("right", "Right failed"),
                false,
                false);

            AssertFails(() => Assert(result.Or(right)), expected);
        }

        [Test]
        public void Throws_should_fail_when_wrong_type_thrown()
        {
            NUnit.Framework.Assert.Throws<SharpAssertionException>(() => 
                Throws<NullReferenceException>(() => throw new ArgumentException()));
        }

        [Test]
        public void Throws_should_fail_when_no_exception_thrown()
        {
            var result = Throws<ArgumentException>(() => { });
            var expected = ExpectationResults.Fail(
                "result",
                $"Expected exception of type '{typeof(ArgumentException).FullName}', but no exception was thrown");

            AssertFails(() => Assert(result), expected);
            AssertPasses(() => Assert(!result));
            
            NUnit.Framework.Assert.Throws<InvalidOperationException>(() => _ = result.Exception);
        }
    }

    [TestFixture]
    class ThrowsAsyncTests
    {
        [Test]
        public async Task Assert_should_support_awaited_ThrowsAsync_expectation()
        {
            Func<Task> action = async () =>
                Assert(await ThrowsAsync<InvalidOperationException>(() =>
                    Task.FromException(new InvalidOperationException("boom"))));

            await action.Should().NotThrowAsync();
        }

        [Test]
        public async Task Assert_should_fail_for_awaited_ThrowsAsync_expectation_when_no_exception()
        {
            var expected = ExpectationResults.Fail(
                "await ThrowsAsync<ArgumentException>(() => Task.CompletedTask)",
                $"Expected exception of type '{typeof(ArgumentException).FullName}', but no exception was thrown");

            await AssertFailsAsync(
                async () => Assert(await ThrowsAsync<ArgumentException>(() => Task.CompletedTask)),
                expected);
        }

        [Test]
        public async Task ThrowsAsync_should_pass_when_expected_exception()
        {
            var result = await ThrowsAsync<InvalidOperationException>(async () => 
            {
                await Task.Yield();
                throw new InvalidOperationException();
            });

            Action action = () => Assert(result);
            action.Should().NotThrow();
        }

        [Test]
        public async Task ThrowsAsync_should_fail_wrong_type()
        {
            var action = async () => await ThrowsAsync<NullReferenceException>(async () => 
            {
                await Task.Yield();
                throw new ArgumentException();
            });

            await action.Should().ThrowAsync<SharpAssertionException>();
        }
    }

    sealed class ThrowingExpectation : Expectation
    {
        public override EvaluationResult Evaluate(ExpectationContext context) =>
            throw new InvalidOperationException("Should not be evaluated");
    }

    sealed class FailingExpectation(string message) : Expectation
    {
        public override EvaluationResult Evaluate(ExpectationContext context) =>
            ExpectationResults.Fail(context.Expression, message);
    }
}
