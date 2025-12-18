// ABOUTME: Tests for Exactly quantifier expectation following TDD
// ABOUTME: Validates that Exactly(n) asserts exactly N items satisfy the inner expectation

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections.Quantifiers;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Collections.Quantifiers;

[TestFixture]
public class ExactlyExpectationFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_exactly_N_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            AssertPasses(() => Assert(numbers.Exactly(2, x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_fewer_than_N_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 2, 3, 5, 7 };

            var act = () => Assert(numbers.Exactly(3, x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>()
               .Where(e => e.Message.Contains("1") && e.Message.Contains("5"));
        }

        [Test]
        public void Should_fail_when_more_than_N_items_satisfy_expectation()
        {
            var numbers = new[] { 2, 4, 6, 8 };

            var act = () => Assert(numbers.Exactly(2, x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>()
               .Where(e => e.Message.Contains("[0]") && e.Message.Contains("[1]"));
        }

        [Test]
        public void Should_pass_for_empty_collection_with_count_zero()
        {
            var numbers = Array.Empty<int>();

            AssertPasses(() => Assert(numbers.Exactly(0, x => x.IsEven())));
        }

        [Test]
        public void Should_fail_for_empty_collection_with_count_one()
        {
            var numbers = Array.Empty<int>();

            var act = () => Assert(numbers.Exactly(1, x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_behave_like_None_when_count_is_zero()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(numbers.Exactly(0, x => x.IsEven())));
        }

        [Test]
        public void Should_behave_like_One_when_count_is_one()
        {
            var numbers = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(numbers.Exactly(1, x => x.IsEven())));
        }

        [Test]
        public void Should_work_with_nested_collections()
        {
            var matrix = new[] { new[] { 2, 4 }, new[] { 1, 3 }, new[] { 6, 8 } };

            AssertPasses(() => Assert(matrix.Exactly(2, row => row.Each(x => x.IsEven()))));
        }

        [Test]
        public void Should_work_with_bool_predicate()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            AssertPasses(() => Assert(numbers.Exactly(2, x => x % 2 == 0)));
        }

        [Test]
        public void Should_support_negation()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(!numbers.Exactly(2, x => x.IsEven())));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var numbers1 = new[] { 1, 2, 3, 4, 5 };
            var numbers2 = new[] { -1, 0, 1, 2 };

            AssertPasses(() => Assert(
                numbers1.Exactly(2, x => x.IsEven()) &
                numbers2.Exactly(2, x => x.IsPositive())
            ));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };
            var expectation = numbers.Exactly(2, x => x.IsEven());
            var context = TestContext("numbers.Exactly(2, x => x.IsEven())");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_show_failures_when_too_few_matched()
        {
            var result = new CollectionQuantifierResult(
                "numbers.Exactly(3, x => x.IsEven())",
                "exactly 3",
                5,
                1,
                4,
                Passed: false,
                [
                    (0, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 1")),
                    (2, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 3")),
                    (3, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 5")),
                    (4, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 7"))
                ]);

            AssertRendersExactly(result,
                "False",
                "Expected exactly 3 item to satisfy expectation, but 4 of 5 failed:",
                "[0]: False",
                "Expected even number, got 1",
                "[2]: False",
                "Expected even number, got 3",
                "[3]: False",
                "Expected even number, got 5",
                "[4]: False",
                "Expected even number, got 7");
        }

        [Test]
        public void Should_show_extra_matches_when_too_many_matched()
        {
            var result = new CollectionQuantifierResult(
                "numbers.Exactly(2, x => x.IsEven())",
                "exactly 2",
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
                "Expected exactly 2 item to satisfy expectation, but 4 of 4 failed:",
                "[0]: True",
                "[1]: True",
                "[2]: True",
                "[3]: True");
        }
    }
}
