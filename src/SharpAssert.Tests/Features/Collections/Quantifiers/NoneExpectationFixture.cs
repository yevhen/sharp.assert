// ABOUTME: Tests for None quantifier expectation following TDD
// ABOUTME: Validates that None() asserts no items satisfy the inner expectation

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections.Quantifiers;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Collections.Quantifiers;

[TestFixture]
public class NoneExpectationFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_no_items_satisfy_expectation()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(numbers.None(x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_any_item_satisfies_expectation()
        {
            var numbers = new[] { 1, 2, 3 };

            var act = () => Assert(numbers.None(x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_for_empty_collection()
        {
            var numbers = Array.Empty<int>();

            AssertPasses(() => Assert(numbers.None(x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_multiple_items_satisfy()
        {
            var numbers = new[] { 2, 4, 6 };

            var act = () => Assert(numbers.None(x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_show_passing_items_as_violations()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            var act = () => Assert(numbers.None(x => x.IsEven()));

            act.Should().Throw<SharpAssertionException>()
               .Where(e => e.Message.Contains("[1]") && e.Message.Contains("[3]"));
        }

        [Test]
        public void Should_work_with_nested_collections()
        {
            var matrix = new[] { new[] { 1, 3 }, new[] { 5, 7 } };

            AssertPasses(() => Assert(matrix.None(row => row.Some(x => x.IsEven()))));
        }

        [Test]
        public void Should_work_with_bool_predicate()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(numbers.None(x => x % 2 == 0)));
        }

        [Test]
        public void Should_fail_bool_predicate_with_diagnostic()
        {
            var numbers = new[] { 1, 2, 3 };

            var act = () => Assert(numbers.None(x => x % 2 == 0));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_negation()
        {
            var numbers = new[] { 2, 4, 6 };

            AssertPasses(() => Assert(!numbers.None(x => x.IsEven())));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var odds = new[] { 1, 3, 5 };
            var negatives = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(
                odds.None(x => x.IsEven()) &
                negatives.None(x => x.IsNegative())
            ));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var numbers = new[] { 1, 3, 5 };
            var expectation = numbers.None(x => x.IsEven());
            var context = TestContext("numbers.None(x => x.IsEven())");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_show_unexpected_matches_as_violations()
        {
            var result = new CollectionQuantifierResult(
                "numbers.None(x => x.IsEven())",
                "none",
                5,
                2,
                2,
                Passed: false,
                [
                    (1, ExpectationResults.Pass("x.IsEven()")),
                    (3, ExpectationResults.Pass("x.IsEven()"))
                ]);

            AssertRendersExactly(result,
                "False",
                "Expected none item to satisfy expectation, but 2 of 5 failed:",
                "[1]: True",
                "[3]: True");
        }

        [Test]
        public void Should_include_quantifier_summary()
        {
            var result = new CollectionQuantifierResult(
                "items.None(...)",
                "none",
                5,
                3,
                3,
                Passed: false,
                [
                    (0, ExpectationResults.Pass("x")),
                    (2, ExpectationResults.Pass("x")),
                    (4, ExpectationResults.Pass("x"))
                ]);

            var rendered = Rendered(result);

            rendered.Should().Contain("3 of 5 failed");
        }
    }
}

public static class NoneTestExtensions
{
    public static Expectation IsNegative(this int value) =>
        Expectation.From(
            () => value < 0,
            () => [$"Expected negative number, got {value}"]);
}
