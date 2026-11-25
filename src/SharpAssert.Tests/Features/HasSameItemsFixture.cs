// ABOUTME: Tests for HasSameItems expectation
// ABOUTME: Validates that two collections have the same items in any order, duplicates counted

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class HasSameItemsFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_items_are_in_another_order()
        {
            var collection = new List<int> { 3, 1, 2 };
            var expected = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.HasSameItems(expected)));
        }

        [Test]
        public void Should_fail_when_duplicates_differ()
        {
            var collection = new[] { 1, 1, 2 };

            var act = () => Assert(collection.HasSameItems(1, 2, 2));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_compare_items_with_comparer()
        {
            var collection = new[] { "A", "b" };
            var expected = new[] { "B", "a" };

            AssertPasses(() => Assert(collection.HasSameItems(expected, StringComparer.OrdinalIgnoreCase)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_missing_and_extra_items()
        {
            var collection = new[] { 1, 2, 4 };
            var expectation = collection.HasSameItems(3, 2, 1);
            var context = TestContext("collection.HasSameItems(3, 2, 1)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected the same items as [3, 2, 1], but found [1, 2, 4]: missing [3], extra [4].");
        }

        [Test]
        public void Should_render_only_missing_items()
        {
            var collection = new[] { 1 };
            var expectation = collection.HasSameItems(1, 2);
            var context = TestContext("collection.HasSameItems(1, 2)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected the same items as [1, 2], but found [1]: missing [2].");
        }
    }
}
