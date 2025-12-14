// ABOUTME: Tests for collection ordering expectations
// ABOUTME: Validates IsInAscendingOrder/IsInDescendingOrder functionality

using NUnit.Framework;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class CollectionOrderingFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_for_ascending_order()
        {
            var collection = new[] { 1, 2, 3, 4 };

            AssertPasses(() => Assert(collection.IsInAscendingOrder()));
        }


        [Test]
        public void Should_pass_for_empty_collection()
        {
            var collection = new int[] { };

            AssertPasses(() => Assert(collection.IsInAscendingOrder()));
        }

        [Test]
        public void Should_pass_for_single_element()
        {
            var collection = new[] { 42 };

            AssertPasses(() => Assert(collection.IsInAscendingOrder()));
        }

        [Test]
        public void Should_allow_equal_consecutive_elements()
        {
            var collection = new[] { 1, 2, 2, 3 };

            AssertPasses(() => Assert(collection.IsInAscendingOrder()));
        }

        [Test]
        public void Should_pass_for_descending_order()
        {
            var collection = new[] { 4, 3, 2, 1 };

            AssertPasses(() => Assert(collection.IsInDescendingOrder()));
        }

        [Test]
        public void Should_pass_for_descending_with_equal_elements()
        {
            var collection = new[] { 4, 3, 3, 1 };

            AssertPasses(() => Assert(collection.IsInDescendingOrder()));
        }

        [Test]
        public void Should_pass_with_custom_comparer()
        {
            var collection = new[] { "z", "b", "a" };
            var comparer = StringComparer.OrdinalIgnoreCase;

            AssertPasses(() => Assert(collection.IsInDescendingOrder(comparer)));
        }

        [Test]
        public void Should_pass_descending_empty_collection()
        {
            var collection = new int[] { };

            AssertPasses(() => Assert(collection.IsInDescendingOrder()));
        }

        [Test]
        public void Should_pass_descending_single_element()
        {
            var collection = new[] { 42 };

            AssertPasses(() => Assert(collection.IsInDescendingOrder()));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_ascending_violation()
        {
            var collection = new[] { 1, 2, 5, 3 };
            var expectation = collection.IsInAscendingOrder();
            var context = new ExpectationContext("collection.IsInAscendingOrder()", "test.cs", 1, null, default);

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to be in ascending order, but found item at index 2 is in wrong order.");
        }

        [Test]
        public void Should_render_descending_violation()
        {
            var collection = new[] { 5, 3, 4, 1 };
            var expectation = collection.IsInDescendingOrder();
            var context = new ExpectationContext("collection.IsInDescendingOrder()", "test.cs", 1, null, default);

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to be in descending order, but found item at index 1 is in wrong order.");
        }

        [Test]
        public void Should_render_pass_message()
        {
            var collection = new[] { 1, 2, 3, 4 };
            var expectation = collection.IsInAscendingOrder();
            var context = new ExpectationContext("collection.IsInAscendingOrder()", "test.cs", 1, null, default);

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }
    }
}
