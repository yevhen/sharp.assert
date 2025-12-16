// ABOUTME: Tests for set operation expectations (IsSubsetOf, IsSupersetOf, Intersects)
// ABOUTME: Validates subset, superset, and intersection checks for collections

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class SetOperationsFixture : TestBase
{
    [TestFixture]
    class IsSubsetOfTests
    {
        [Test]
        public void Should_pass_when_subset()
        {
            var collection = new[] { 1, 2 };
            var superset = new[] { 1, 2, 3, 4 };

            AssertPasses(() => Assert(collection.IsSubsetOf(superset)));
        }

        [Test]
        public void Should_pass_when_equal_sets()
        {
            var collection = new[] { 1, 2, 3 };
            var superset = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.IsSubsetOf(superset)));
        }

        [Test]
        public void Should_pass_for_empty_collection()
        {
            var collection = Array.Empty<int>();
            var superset = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.IsSubsetOf(superset)));
        }

        [Test]
        public void Should_pass_for_both_empty()
        {
            var collection = Array.Empty<int>();
            var superset = Array.Empty<int>();

            AssertPasses(() => Assert(collection.IsSubsetOf(superset)));
        }

        [Test]
        public void Should_fail_when_not_subset()
        {
            var collection = new[] { 1, 2, 99 };
            var superset = new[] { 1, 2, 3 };

            var act = () => Assert(collection.IsSubsetOf(superset));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_ignore_duplicates()
        {
            var collection = new[] { 1, 1, 2, 2 };
            var superset = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.IsSubsetOf(superset)));
        }
    }

    [TestFixture]
    class IsSupersetOfTests
    {
        [Test]
        public void Should_pass_when_superset()
        {
            var collection = new[] { 1, 2, 3, 4 };
            var subset = new[] { 1, 2 };

            AssertPasses(() => Assert(collection.IsSupersetOf(subset)));
        }

        [Test]
        public void Should_pass_when_equal_sets()
        {
            var collection = new[] { 1, 2, 3 };
            var subset = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.IsSupersetOf(subset)));
        }

        [Test]
        public void Should_pass_for_empty_subset()
        {
            var collection = new[] { 1, 2, 3 };
            var subset = Array.Empty<int>();

            AssertPasses(() => Assert(collection.IsSupersetOf(subset)));
        }

        [Test]
        public void Should_fail_when_not_superset()
        {
            var collection = new[] { 1, 2, 3 };
            var subset = new[] { 1, 2, 99 };

            var act = () => Assert(collection.IsSupersetOf(subset));
            act.Should().Throw<SharpAssertionException>();
        }
    }

    [TestFixture]
    class IntersectsTests
    {
        [Test]
        public void Should_pass_when_has_intersection()
        {
            var collection = new[] { 1, 2, 3 };
            var other = new[] { 3, 4, 5 };

            AssertPasses(() => Assert(collection.Intersects(other)));
        }

        [Test]
        public void Should_pass_when_complete_overlap()
        {
            var collection = new[] { 1, 2, 3 };
            var other = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.Intersects(other)));
        }

        [Test]
        public void Should_fail_when_no_intersection()
        {
            var collection = new[] { 1, 2, 3 };
            var other = new[] { 4, 5, 6 };

            var act = () => Assert(collection.Intersects(other));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_empty_collection()
        {
            var collection = Array.Empty<int>();
            var other = new[] { 1, 2, 3 };

            var act = () => Assert(collection.Intersects(other));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_other_empty()
        {
            var collection = new[] { 1, 2, 3 };
            var other = Array.Empty<int>();

            var act = () => Assert(collection.Intersects(other));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_negation_for_no_intersection()
        {
            var collection = new[] { 1, 2, 3 };
            var other = new[] { 4, 5, 6 };

            AssertPasses(() => Assert(!collection.Intersects(other)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_subset_pass()
        {
            var collection = new[] { 1, 2 };
            var superset = new[] { 1, 2, 3 };
            var expectation = collection.IsSubsetOf(superset);
            var context = TestContext("collection.IsSubsetOf(superset)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_render_subset_failure()
        {
            var collection = new[] { 1, 2, 99 };
            var superset = new[] { 1, 2, 3 };
            var expectation = collection.IsSubsetOf(superset);
            var context = TestContext("collection.IsSubsetOf(superset)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to be a subset, but element 99 is not in the superset.");
        }

        [Test]
        public void Should_render_superset_failure()
        {
            var collection = new[] { 1, 2, 3 };
            var subset = new[] { 1, 2, 99 };
            var expectation = collection.IsSupersetOf(subset);
            var context = TestContext("collection.IsSupersetOf(subset)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to be a superset, but element 99 from subset is missing.");
        }

        [Test]
        public void Should_render_intersects_failure()
        {
            var collection = new[] { 1, 2, 3 };
            var other = new[] { 4, 5, 6 };
            var expectation = collection.Intersects(other);
            var context = TestContext("collection.Intersects(other)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collections to have at least one common element, but no intersection found.");
        }
    }
}
