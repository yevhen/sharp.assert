// ABOUTME: Tests for AtLeast quantifier expectation following TDD
// ABOUTME: Validates that AtLeast(n) asserts at least N items satisfy the inner expectation

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections.Quantifiers;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Collections.Quantifiers;

[TestFixture]
public class AtLeastExpectationFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_at_least_N_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            AssertPasses(() => Assert(numbers.AtLeast(2, x => x.IsEven())));
        }

        [Test]
        public void Should_pass_when_more_than_N_items_satisfy_expectation()
        {
            var numbers = new[] { 2, 4, 6, 8 };

            AssertPasses(() => Assert(numbers.AtLeast(2, x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_fewer_than_N_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 2, 3, 5, 7 };

            var act = () => Assert(numbers.AtLeast(3, x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>()
               .Where(e => e.Message.Contains("4 of 5 failed"));
        }

        [Test]
        public void Should_pass_for_empty_collection_with_count_zero()
        {
            var numbers = Array.Empty<int>();

            AssertPasses(() => Assert(numbers.AtLeast(0, x => x.IsEven())));
        }

        [Test]
        public void Should_fail_for_empty_collection_with_count_one()
        {
            var numbers = Array.Empty<int>();

            var act = () => Assert(numbers.AtLeast(1, x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_always_pass_when_count_is_zero()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(numbers.AtLeast(0, x => x.IsEven())));
        }

        [Test]
        public void Should_behave_like_Some_when_count_is_one()
        {
            var numbers = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(numbers.AtLeast(1, x => x.IsEven())));
        }

        [Test]
        public void Should_work_with_nested_collections()
        {
            var matrix = new[] { new[] { 2, 4 }, new[] { 1, 3 }, new[] { 6, 8 } };

            AssertPasses(() => Assert(matrix.AtLeast(2, row => row.Each(x => x.IsEven()))));
        }

        [Test]
        public void Should_work_with_bool_predicate()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            AssertPasses(() => Assert(numbers.AtLeast(2, x => x % 2 == 0)));
        }

        [Test]
        public void Should_support_negation()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(!numbers.AtLeast(2, x => x.IsEven())));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var numbers1 = new[] { 1, 2, 3, 4, 5 };
            var numbers2 = new[] { -1, 0, 1, 2 };

            AssertPasses(() => Assert(
                numbers1.AtLeast(2, x => x.IsEven()) &
                numbers2.AtLeast(1, x => x.IsPositive())
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
            var expectation = numbers.AtLeast(2, x => x.IsEven());
            var context = TestContext("numbers.AtLeast(2, x => x.IsEven())");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_show_failures_when_too_few_matched()
        {
            var result = new CollectionQuantifierResult(
                "numbers.AtLeast(3, x => x.IsEven())",
                "at least 3",
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
                "Expected at least 3 item to satisfy expectation, but 4 of 5 failed:",
                "[0]: False",
                "Expected even number, got 1",
                "[2]: False",
                "Expected even number, got 3",
                "[3]: False",
                "Expected even number, got 5",
                "[4]: False",
                "Expected even number, got 7");
        }
    }
}
