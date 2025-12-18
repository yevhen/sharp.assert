// ABOUTME: Tests for AtMost quantifier expectation following TDD
// ABOUTME: Validates that AtMost(n) asserts at most N items satisfy the inner expectation

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections.Quantifiers;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Collections.Quantifiers;

[TestFixture]
public class AtMostExpectationFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_at_most_N_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 2, 3, 5, 7 };

            AssertPasses(() => Assert(numbers.AtMost(2, x => x.IsEven())));
        }

        [Test]
        public void Should_pass_when_fewer_than_N_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 3, 5, 7 };

            AssertPasses(() => Assert(numbers.AtMost(2, x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_more_than_N_items_satisfy_expectation()
        {
            var numbers = new[] { 2, 4, 6, 8 };

            var act = () => Assert(numbers.AtMost(2, x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>()
               .Where(e => e.Message.Contains("[0]") && e.Message.Contains("[1]"));
        }

        [Test]
        public void Should_pass_for_empty_collection()
        {
            var numbers = Array.Empty<int>();

            AssertPasses(() => Assert(numbers.AtMost(2, x => x.IsEven())));
        }

        [Test]
        public void Should_behave_like_None_when_count_is_zero()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(numbers.AtMost(0, x => x.IsEven())));
        }

        [Test]
        public void Should_fail_like_None_when_count_is_zero_and_any_match()
        {
            var numbers = new[] { 1, 2, 3 };

            var act = () => Assert(numbers.AtMost(0, x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_work_with_nested_collections()
        {
            var matrix = new[] { new[] { 2, 4 }, new[] { 1, 3 }, new[] { 5, 7 } };

            AssertPasses(() => Assert(matrix.AtMost(1, row => row.Each(x => x.IsEven()))));
        }

        [Test]
        public void Should_work_with_bool_predicate()
        {
            var numbers = new[] { 1, 2, 3, 5, 7 };

            AssertPasses(() => Assert(numbers.AtMost(2, x => x % 2 == 0)));
        }

        [Test]
        public void Should_support_negation()
        {
            var numbers = new[] { 2, 4, 6 };

            AssertPasses(() => Assert(!numbers.AtMost(1, x => x.IsEven())));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var numbers1 = new[] { 1, 2, 3, 5, 7 };
            var numbers2 = new[] { -1, -2, 0, 1 };

            AssertPasses(() => Assert(
                numbers1.AtMost(2, x => x.IsEven()) &
                numbers2.AtMost(2, x => x.IsPositive())
            ));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var numbers = new[] { 1, 2, 3, 5, 7 };
            var expectation = numbers.AtMost(2, x => x.IsEven());
            var context = TestContext("numbers.AtMost(2, x => x.IsEven())");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_show_extra_matches_when_too_many_matched()
        {
            var result = new CollectionQuantifierResult(
                "numbers.AtMost(2, x => x.IsEven())",
                "at most 2",
                4,
                4,
                4,
                Passed: false,
                [
                    (0, ExpectationResults.Pass("x.IsEven()")),
                    (1, ExpectationResults.Pass("x.IsEven()")),
                    (2, ExpectationResults.Pass("x.IsEven()")),
                    (3, ExpectationResults.Pass("x.IsEven()"))
                ]);

            AssertRendersExactly(result,
                "False",
                "Expected at most 2 item to satisfy expectation, but 4 of 4 failed:",
                "[0]: True",
                "[1]: True",
                "[2]: True",
                "[3]: True");
        }
    }
}
