// ABOUTME: Tests for Some quantifier expectation following TDD
// ABOUTME: Validates that Some() asserts at least one item satisfies the inner expectation

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections.Quantifiers;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Collections.Quantifiers;

[TestFixture]
public class SomeExpectationFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_at_least_one_item_satisfies_expectation()
        {
            var numbers = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(numbers.Some(x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_no_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 3, 5 };

            var expected = new CollectionQuantifierResult(
                "numbers.Some(x => x.IsEven())",
                "some",
                3,
                0,
                3,
                Passed: false,
                [
                    (0, ExpectationResults.Fail("numbers.Some(x => x.IsEven())[0]", "Expected even number, got 1")),
                    (1, ExpectationResults.Fail("numbers.Some(x => x.IsEven())[1]", "Expected even number, got 3")),
                    (2, ExpectationResults.Fail("numbers.Some(x => x.IsEven())[2]", "Expected even number, got 5"))
                ]);

            AssertFails(() => Assert(numbers.Some(x => x.IsEven())), expected);
        }

        [Test]
        public void Should_fail_for_empty_collection()
        {
            var numbers = Array.Empty<int>();

            var act = () => Assert(numbers.Some(x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_multiple_items_satisfy()
        {
            var numbers = new[] { 2, 4, 6 };

            AssertPasses(() => Assert(numbers.Some(x => x.IsEven())));
        }

        [Test]
        public void Should_pass_when_only_last_item_satisfies()
        {
            var numbers = new[] { 1, 3, 4 };

            AssertPasses(() => Assert(numbers.Some(x => x.IsEven())));
        }

        [Test]
        public void Should_work_with_nested_collections()
        {
            var matrix = new[] { new[] { 1, 3 }, new[] { 2, 4 } };

            AssertPasses(() => Assert(matrix.Some(row => row.Some(x => x.IsEven()))));
        }

        [Test]
        public void Should_work_with_bool_predicate()
        {
            var numbers = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(numbers.Some(x => x % 2 == 0)));
        }

        [Test]
        public void Should_fail_bool_predicate_with_diagnostic()
        {
            var numbers = new[] { 1, 3, 5 };

            var act = () => Assert(numbers.Some(x => x % 2 == 0));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_negation()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(!numbers.Some(x => x.IsEven())));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var evens = new[] { 1, 2, 3 };
            var positives = new[] { -1, 0, 1 };

            AssertPasses(() => Assert(
                evens.Some(x => x.IsEven()) &
                positives.Some(x => x.IsPositive())
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
            var expectation = numbers.Some(x => x.IsEven());
            var context = TestContext("numbers.Some(x => x.IsEven())");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_show_all_failing_items()
        {
            var result = new CollectionQuantifierResult(
                "numbers.Some(x => x.IsEven())",
                "some",
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
                "Expected some item to satisfy expectation, but 3 of 3 failed:",
                "[0]: False",
                "Expected even number, got 1",
                "[1]: False",
                "Expected even number, got 3",
                "[2]: False",
                "Expected even number, got 5");
        }

        [Test]
        public void Should_include_quantifier_summary()
        {
            var result = new CollectionQuantifierResult(
                "items.Some(...)",
                "some",
                5,
                0,
                5,
                Passed: false,
                [
                    (0, ExpectationResults.Fail("x", "fail 0")),
                    (1, ExpectationResults.Fail("x", "fail 1")),
                    (2, ExpectationResults.Fail("x", "fail 2")),
                    (3, ExpectationResults.Fail("x", "fail 3")),
                    (4, ExpectationResults.Fail("x", "fail 4"))
                ]);

            var rendered = Rendered(result);

            rendered.Should().Contain("5 of 5 failed");
        }
    }
}
