// ABOUTME: Tests for One quantifier expectation following TDD
// ABOUTME: Validates that One() asserts exactly one item satisfies the inner expectation

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections.Quantifiers;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Collections.Quantifiers;

[TestFixture]
public class OneExpectationFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_exactly_one_item_satisfies_expectation()
        {
            var numbers = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(numbers.One(x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_no_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 3, 5 };

            var act = () => Assert(numbers.One(x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>()
               .Where(e => e.Message.Contains("3 of 3 failed"));
        }

        [Test]
        public void Should_fail_when_multiple_items_satisfy_expectation()
        {
            var numbers = new[] { 2, 4, 6 };

            var act = () => Assert(numbers.One(x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>()
               .Where(e => e.Message.Contains("3"));
        }

        [Test]
        public void Should_fail_for_empty_collection()
        {
            var numbers = Array.Empty<int>();

            var act = () => Assert(numbers.One(x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_first_item_is_only_match()
        {
            var numbers = new[] { 2, 1, 3 };

            AssertPasses(() => Assert(numbers.One(x => x.IsEven())));
        }

        [Test]
        public void Should_pass_when_last_item_is_only_match()
        {
            var numbers = new[] { 1, 3, 4 };

            AssertPasses(() => Assert(numbers.One(x => x.IsEven())));
        }

        [Test]
        public void Should_work_with_nested_collections()
        {
            var matrix = new[] { new[] { 1, 3 }, new[] { 2, 4 }, new[] { 5, 7 } };

            AssertPasses(() => Assert(matrix.One(row => row.Each(x => x.IsEven()))));
        }

        [Test]
        public void Should_work_with_bool_predicate()
        {
            var numbers = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(numbers.One(x => x % 2 == 0)));
        }

        [Test]
        public void Should_fail_bool_predicate_with_diagnostic()
        {
            var numbers = new[] { 1, 3, 5 };

            var act = () => Assert(numbers.One(x => x % 2 == 0));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_negation()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(!numbers.One(x => x.IsEven())));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var mixedEvens = new[] { 1, 2, 3 };
            var mixedPositives = new[] { -1, 0, 1 };

            AssertPasses(() => Assert(
                mixedEvens.One(x => x.IsEven()) &
                mixedPositives.One(x => x.IsPositive())
            ));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var numbers = new[] { 1, 2, 3 };
            var expectation = numbers.One(x => x.IsEven());
            var context = TestContext("numbers.One(x => x.IsEven())");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_show_all_failures_when_none_matched()
        {
            var result = new CollectionQuantifierResult(
                "numbers.One(x => x.IsEven())",
                "one",
                3,
                0,
                3,
                Passed: false,
                [
                    (0, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 1")),
                    (1, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 3")),
                    (2, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 5"))
                ]);

            AssertRendersExactly(result,
                "False",
                "Expected one item to satisfy expectation, but 3 of 3 failed:",
                "[0]: False",
                "Expected even number, got 1",
                "[1]: False",
                "Expected even number, got 3",
                "[2]: False",
                "Expected even number, got 5");
        }

        [Test]
        public void Should_show_extra_matches_when_multiple_matched()
        {
            var result = new CollectionQuantifierResult(
                "numbers.One(x => x.IsEven())",
                "one",
                3,
                3,
                3,
                Passed: false,
                [
                    (0, ExpectationResults.Pass("x.IsEven()")),
                    (1, ExpectationResults.Pass("x.IsEven()")),
                    (2, ExpectationResults.Pass("x.IsEven()"))
                ]);

            AssertRendersExactly(result,
                "False",
                "Expected one item to satisfy expectation, but 3 of 3 failed:",
                "[0]: True",
                "[1]: True",
                "[2]: True");
        }
    }
}
