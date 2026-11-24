// ABOUTME: Tests for IsExactly expectation
// ABOUTME: Validates that a value has exactly the given runtime type, not a subclass

using FluentAssertions;
using NUnit.Framework;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class IsExactlyFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_for_the_exact_type()
        {
            object value = new ArgumentException();

            AssertPasses(() => Assert(value.IsExactly<ArgumentException>()));
        }

        [Test]
        public void Should_fail_for_a_subclass()
        {
            object value = new ArgumentNullException();

            var act = () => Assert(value.IsExactly<ArgumentException>());
            act.Should().Throw<SharpAssertionException>();
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_failure_with_actual_type()
        {
            object value = new ArgumentNullException();
            var expectation = value.IsExactly<ArgumentException>();
            var context = TestContext("value.IsExactly<ArgumentException>()");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected type ArgumentException, but found ArgumentNullException.");
        }

        [Test]
        public void Should_render_failure_for_null()
        {
            object? value = null;
            var expectation = value.IsExactly<ArgumentException>();
            var context = TestContext("value.IsExactly<ArgumentException>()");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected type ArgumentException, but found null.");
        }
    }
}
