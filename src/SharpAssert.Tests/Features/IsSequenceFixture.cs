// ABOUTME: Tests for IsSequence expectation
// ABOUTME: Validates that a collection has exactly the given items in the given order

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class IsSequenceFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_items_match_in_order()
        {
            var collection = new List<int> { 1, 2, 3 };

            AssertPasses(() => Assert(collection.IsSequence(1, 2, 3)));
        }

        [Test]
        public void Should_fail_when_order_differs()
        {
            var collection = new[] { 3, 2, 1 };

            var act = () => Assert(collection.IsSequence(1, 2, 3));
            act.Should().Throw<SharpAssertionException>();
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_failure_with_differing_item()
        {
            var collection = new[] { 1, 2, 4 };
            var expectation = collection.IsSequence(1, 2, 3);
            var context = TestContext("collection.IsSequence(1, 2, 3)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected sequence [1, 2, 3], but found [1, 2, 4]: item at index 2 is 4 instead of 3.");
        }

        [Test]
        public void Should_render_failure_with_different_length()
        {
            var collection = new[] { 1, 2, 3 };
            var expectation = collection.IsSequence(1, 2);
            var context = TestContext("collection.IsSequence(1, 2)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected sequence [1, 2], but found [1, 2, 3]: 3 items instead of 2.");
        }
    }
}
