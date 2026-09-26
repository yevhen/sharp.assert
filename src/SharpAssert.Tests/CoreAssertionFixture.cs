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
        public void Should_preserve_boolean_variable_diagnostics()
        {
            var actual = false;

            AssertFails(() => Assert(actual), Value("actual", false, typeof(bool)));
        }

        [Test]
        public void Should_pass_boolean_variable()
        {
            var actual = true;

            AssertPasses(() => Assert(actual));
        }

        [Test]
        public void Should_evaluate_boolean_property_once_on_failure()
        {
            var counter = new BooleanCounter();

            AssertFails(() => Assert(counter.Value), Value("counter.Value", false, typeof(bool)));
            counter.Calls.Should().Be(1);
        }

        [Test]
        public void Should_preserve_message_for_boolean_property()
        {
            var counter = new BooleanCounter();

            var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(counter.Value, "Property failed"));
            exception.Result!.Context.Message.Should().Be("Property failed");
            counter.Calls.Should().Be(1);
        }

        [Test]
        public void Should_evaluate_argumentless_method_once_on_failure()
        {
            var counter = new BooleanCounter();

            AssertFails(() => Assert(counter.Check()), Value("counter.Check()", false, typeof(bool)));
            counter.Calls.Should().Be(1);
        }

        [Test]
        public void Should_not_repeat_method_argument_when_failure_is_formatted()
        {
            var calls = 0;
            Func<string> needle = () => { calls++; return "missing"; };

            NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert("abc".Contains(needle())));

            calls.Should().Be(1);
        }

        [Test]
        public void Should_preserve_contains_diagnostics_and_argument_order()
        {
            var calls = new List<string>();
            Func<string> source = () => { calls.Add("source"); return "abc"; };
            Func<string> needle = () => { calls.Add("needle"); return "missing"; };

            var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(source().Contains(needle())));

            exception.Result!.Format().Should().Contain("Contains failed: searched for \"missing\" in [a, b, c]");
            calls.Should().Equal("source", "needle");
        }

        [Test]
        public void Should_rewrite_contains_without_expression_tree()
        {
            var source = "using static SharpAssert.Sharp; class Sample { void Check(string value, string needle) { Assert(value.Contains(needle)); } }";

            var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs");

            rewritten.Should().Contain("SharpInternal.AssertContains");
            rewritten.Should().NotContain("SharpInternal.AssertValue");
        }

        [Test]
        public void Should_rewrite_single_argument_method_without_expression_tree()
        {
            var source = "using static SharpAssert.Sharp; class Sample { void Check(string value, string prefix) { Assert(value.StartsWith(prefix)); } }";

            var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs");

            rewritten.Should().Contain("SharpInternal.AssertMethodCall");
            rewritten.Should().NotContain("SharpInternal.AssertValue");
        }

        [Test]
        public void Should_preserve_single_argument_method_diagnostics()
        {
            var value = "abc";
            var prefix = "missing";

            AssertFails(() => Assert(value.StartsWith(prefix)),
                new MethodCallEvaluationResult("value.StartsWith(prefix)", false,
                    [Value("prefix", prefix, typeof(string))]));
        }

        [Test]
        public void Should_rewrite_contains_with_literal_without_expression_tree()
        {
            var source = "using static SharpAssert.Sharp; class Sample { void Check(string value) { Assert(value.Contains(\"arp\")); } }";

            var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs");

            rewritten.Should().Contain("SharpInternal.AssertContains");
        }

        [Test]
        public void Should_rewrite_contains_in_top_level_program()
        {
            var source = "using static SharpAssert.Sharp; System.Console.WriteLine(1); static void Check(string value) { Assert(value.Contains(\"arp\")); }";

            var rewritten = SharpAssertRewriter.Rewrite(source, "Program.cs");

            rewritten.Should().Contain("SharpInternal.AssertContains");
        }

        [Test]
        public void Should_not_repeat_sequence_argument_when_failure_is_formatted()
        {
            var calls = 0;
            Func<int[]> expected = () => { calls++; return [2]; };
            var actual = new[] { 1 };

            NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(Enumerable.SequenceEqual(actual, expected())));

            calls.Should().Be(1);
        }

        [Test]
        public void Should_not_repeat_plain_method_argument_when_failure_is_formatted()
        {
            var calls = 0;
            Func<int> next = () => ++calls;

            NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(IsEven(next())));

            calls.Should().Be(1);
        }

        [Test]
        public void Should_rewrite_argumentless_method_without_expression_tree()
        {
            var source = "using static SharpAssert.Sharp; class Sample { bool Check() => true; void Verify() { Assert(Check()); } }";

            var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs");

            rewritten.Should().Contain("SharpInternal.AssertBoolean");
            rewritten.Should().NotContain("SharpInternal.AssertValue");
        }

        [Test]
        public void Should_rewrite_boolean_property_without_expression_tree()
        {
            var source = "using static SharpAssert.Sharp; class Sample { bool Value => true; void Check(Sample counter) { Assert(counter.Value); } }";

            var rewritten = SharpAssertRewriter.Rewrite(source, "Sample.cs");

            rewritten.Should().Contain("SharpInternal.AssertBoolean");
            rewritten.Should().NotContain("SharpInternal.AssertValue");
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

    static bool IsEven(int value) => value % 2 == 0;

    sealed class BooleanCounter
    {
        public int Calls { get; private set; }
        public bool Value => Check();

        public bool Check()
        {
            Calls++;
            return false;
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
