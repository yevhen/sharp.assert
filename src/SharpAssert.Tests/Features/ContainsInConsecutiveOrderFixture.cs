// ABOUTME: Tests for ContainsInConsecutiveOrder expectation
// ABOUTME: Validates that elements appear consecutively (no gaps)

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class ContainsInConsecutiveOrderFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_elements_consecutive_at_start()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.ContainsInConsecutiveOrder(expected)));
        }

        [Test]
        public void Should_pass_when_elements_consecutive_in_middle()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 2, 3, 4 };

            AssertPasses(() => Assert(collection.ContainsInConsecutiveOrder(expected)));
        }

        [Test]
        public void Should_pass_when_elements_consecutive_at_end()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 3, 4, 5 };

            AssertPasses(() => Assert(collection.ContainsInConsecutiveOrder(expected)));
        }

        [Test]
        public void Should_pass_for_empty_expected()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = Array.Empty<int>();

            AssertPasses(() => Assert(collection.ContainsInConsecutiveOrder(expected)));
        }

        [Test]
        public void Should_pass_for_single_element_match()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 2 };

            AssertPasses(() => Assert(collection.ContainsInConsecutiveOrder(expected)));
        }

        [Test]
        public void Should_pass_for_exact_match()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.ContainsInConsecutiveOrder(expected)));
        }

        [Test]
        public void Should_fail_when_elements_have_gaps()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 3, 5 };

            var act = () => Assert(collection.ContainsInConsecutiveOrder(expected));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_element_missing()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 99, 3 };

            var act = () => Assert(collection.ContainsInConsecutiveOrder(expected));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_collection_empty()
        {
            var collection = Array.Empty<int>();
            var expected = new[] { 1 };

            var act = () => Assert(collection.ContainsInConsecutiveOrder(expected));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_order_wrong()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 3, 2 };

            var act = () => Assert(collection.ContainsInConsecutiveOrder(expected));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var collection1 = new[] { 1, 2, 3 };
            var collection2 = new[] { 4, 5, 6 };

            AssertPasses(() => Assert(
                collection1.ContainsInConsecutiveOrder(new[] { 1, 2 }) &
                collection2.ContainsInConsecutiveOrder(new[] { 5, 6 })));
        }

        [Test]
        public void Should_support_negation()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 3 };

            AssertPasses(() => Assert(!collection.ContainsInConsecutiveOrder(expected)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 2 };
            var expectation = collection.ContainsInConsecutiveOrder(expected);
            var context = TestContext("collection.ContainsInConsecutiveOrder(expected)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_render_failure_with_gap()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 3 };
            var expectation = collection.ContainsInConsecutiveOrder(expected);
            var context = TestContext("collection.ContainsInConsecutiveOrder(expected)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to contain [1, 3] in consecutive order, but sequence was not found.");
        }

        [Test]
        public void Should_render_failure_with_missing_element()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 99 };
            var expectation = collection.ContainsInConsecutiveOrder(expected);
            var context = TestContext("collection.ContainsInConsecutiveOrder(expected)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to contain [1, 99] in consecutive order, but sequence was not found.");
        }
    }
}
