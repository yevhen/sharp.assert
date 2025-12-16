// ABOUTME: Tests for ContainsInOrder expectation
// ABOUTME: Validates that elements appear in sequence (gaps allowed)

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class ContainsInOrderFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_elements_in_exact_order()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.ContainsInOrder(expected)));
        }

        [Test]
        public void Should_pass_when_elements_in_order_with_gaps()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(collection.ContainsInOrder(expected)));
        }

        [Test]
        public void Should_pass_for_empty_expected()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = Array.Empty<int>();

            AssertPasses(() => Assert(collection.ContainsInOrder(expected)));
        }

        [Test]
        public void Should_pass_for_single_element_match()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 2 };

            AssertPasses(() => Assert(collection.ContainsInOrder(expected)));
        }

        [Test]
        public void Should_pass_for_exact_match()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.ContainsInOrder(expected)));
        }

        [Test]
        public void Should_fail_when_order_wrong()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var expected = new[] { 3, 1 };

            var act = () => Assert(collection.ContainsInOrder(expected));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_element_missing()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 99, 3 };

            var act = () => Assert(collection.ContainsInOrder(expected));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_collection_empty()
        {
            var collection = Array.Empty<int>();
            var expected = new[] { 1 };

            var act = () => Assert(collection.ContainsInOrder(expected));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var collection1 = new[] { 1, 2, 3 };
            var collection2 = new[] { 4, 5, 6 };

            AssertPasses(() => Assert(
                collection1.ContainsInOrder(new[] { 1, 3 }) &
                collection2.ContainsInOrder(new[] { 4, 6 })));
        }

        [Test]
        public void Should_support_negation()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 3, 1 };

            AssertPasses(() => Assert(!collection.ContainsInOrder(expected)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 3 };
            var expectation = collection.ContainsInOrder(expected);
            var context = TestContext("collection.ContainsInOrder(expected)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_render_failure_with_missing_element()
        {
            var collection = new[] { 1, 2, 3 };
            var expected = new[] { 1, 99 };
            var expectation = collection.ContainsInOrder(expected);
            var context = TestContext("collection.ContainsInOrder(expected)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to contain [1, 99] in order, but element 99 was not found after 1.");
        }

        [Test]
        public void Should_render_failure_with_wrong_order()
        {
            var collection = new[] { 3, 2, 1 };
            var expected = new[] { 1, 3 };
            var expectation = collection.ContainsInOrder(expected);
            var context = TestContext("collection.ContainsInOrder(expected)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to contain [1, 3] in order, but element 3 was not found after 1.");
        }
    }
}
