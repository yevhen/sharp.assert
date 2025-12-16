// ABOUTME: Tests for Satisfies expectation with bipartite matching
// ABOUTME: Validates that collection elements can satisfy predicates with 1:1 matching

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class SatisfiesFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_each_predicate_matches_different_element()
        {
            var collection = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.Satisfies(
                x => x == 1,
                x => x == 2,
                x => x == 3
            )));
        }

        [Test]
        public void Should_pass_when_predicates_can_be_satisfied_in_different_order()
        {
            var collection = new[] { 3, 1, 2 };

            AssertPasses(() => Assert(collection.Satisfies(
                x => x == 1,
                x => x == 2,
                x => x == 3
            )));
        }

        [Test]
        public void Should_pass_when_more_elements_than_predicates()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };

            AssertPasses(() => Assert(collection.Satisfies(
                x => x > 3,
                x => x < 3
            )));
        }

        [Test]
        public void Should_pass_for_empty_predicates()
        {
            var collection = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(collection.Satisfies<int>()));
        }

        [Test]
        public void Should_fail_when_same_element_needed_for_multiple_predicates()
        {
            var collection = new[] { 1, 2, 3 };

            var act = () => Assert(collection.Satisfies(
                x => x == 1,
                x => x == 1  // Same element needed twice
            ));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_no_element_matches_predicate()
        {
            var collection = new[] { 1, 2, 3 };

            var act = () => Assert(collection.Satisfies(
                x => x == 1,
                x => x == 99
            ));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_collection_empty_but_predicates_provided()
        {
            var collection = Array.Empty<int>();

            var act = () => Assert(collection.Satisfies(
                x => x > 0
            ));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_handle_overlapping_predicates_correctly()
        {
            // Alice (age 25) could match both predicates, but Bob (age 30) exists
            // This should pass because Alice matches "age > 20" and Bob matches "age > 25"
            var people = new[]
            {
                new Person("Alice", 25),
                new Person("Bob", 30)
            };

            AssertPasses(() => Assert(people.Satisfies(
                p => p.Age > 20,
                p => p.Age > 25
            )));
        }

        [Test]
        public void Should_fail_when_bipartite_matching_impossible()
        {
            // Alice matches both predicates, but we need 2 different people
            // Charlie doesn't match any predicate
            var people = new[]
            {
                new Person("Alice", 30),
                new Person("Charlie", 10)
            };

            var act = () => Assert(people.Satisfies(
                p => p.Age > 20,
                p => p.Age > 25
            ));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_negation()
        {
            var collection = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(!collection.Satisfies(
                x => x == 99,
                x => x == 100
            )));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var collection1 = new[] { 1, 2, 3 };
            var collection2 = new[] { 4, 5, 6 };

            AssertPasses(() => Assert(
                collection1.Satisfies(x => x == 1) &
                collection2.Satisfies(x => x == 5)
            ));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var collection = new[] { 1, 2, 3 };
            var expectation = collection.Satisfies(x => x == 1, x => x == 2);
            var context = TestContext("collection.Satisfies(...)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_render_failure()
        {
            var collection = new[] { 1, 2, 3 };
            var expectation = collection.Satisfies(x => x == 1, x => x == 1);
            var context = TestContext("collection.Satisfies(...)");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected collection to satisfy all predicates with unique elements, but no valid matching exists.");
        }
    }

    record Person(string Name, int Age);
}
