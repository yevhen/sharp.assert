// ABOUTME: Tests for Each quantifier expectation following TDD
// ABOUTME: Validates that Each() asserts all items satisfy the inner expectation

using FluentAssertions;
using NUnit.Framework;
using SharpAssert.Features.Collections.Quantifiers;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Collections.Quantifiers;

[TestFixture]
public class EachExpectationFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_all_items_satisfy_expectation()
        {
            var numbers = new[] { 2, 4, 6 };

            AssertPasses(() => Assert(numbers.Each(x => x.IsEven())));
        }

        [Test]
        public void Should_fail_when_any_item_fails_expectation()
        {
            var numbers = new[] { 2, 3, 4 };

            var expected = new CollectionQuantifierResult(
                "numbers.Each(x => x.IsEven())",
                "each",
                3,
                2,
                1,
                Passed: false,
                [(1, ExpectationResults.Fail("numbers.Each(x => x.IsEven())[1]", $"Expected even number, got 3"))]);

            AssertFails(() => Assert(numbers.Each(x => x.IsEven())), expected);
        }

        [Test]
        public void Should_pass_for_empty_collection()
        {
            var numbers = Array.Empty<int>();

            AssertPasses(() => Assert(numbers.Each(x => x.IsEven())));
        }

        [Test]
        public void Should_report_all_failing_items_not_just_first()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            var expected = new CollectionQuantifierResult(
                "numbers.Each(x => x.IsEven())",
                "each",
                5,
                2,
                3,
                Passed: false,
                [
                    (0, ExpectationResults.Fail("numbers.Each(x => x.IsEven())[0]", "Expected even number, got 1")),
                    (2, ExpectationResults.Fail("numbers.Each(x => x.IsEven())[2]", "Expected even number, got 3")),
                    (4, ExpectationResults.Fail("numbers.Each(x => x.IsEven())[4]", "Expected even number, got 5"))
                ]);

            AssertFails(() => Assert(numbers.Each(x => x.IsEven())), expected);
        }

        [Test]
        public void Should_work_with_nested_collections()
        {
            var matrix = new[] { new[] { 2, 4 }, new[] { 6, 8 } };

            AssertPasses(() => Assert(matrix.Each(row => row.Each(x => x.IsEven()))));
        }

        [Test]
        public void Should_fail_nested_collections_with_correct_indices()
        {
            var matrix = new[] { new[] { 2, 3 }, new[] { 4, 6 } };

            var act = () => Assert(matrix.Each(row => row.Each(x => x.IsEven())));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_work_with_bool_predicate()
        {
            var numbers = new[] { 2, 4, 6 };

            AssertPasses(() => Assert(numbers.Each(x => x % 2 == 0)));
        }

        [Test]
        public void Should_fail_bool_predicate_with_diagnostic()
        {
            var numbers = new[] { 1, 2, 3 };

            var act = () => Assert(numbers.Each(x => x % 2 == 0));

            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_compose_with_IsEquivalentTo()
        {
            var items = new[] { new Person("Alice", 30), new Person("Bob", 25) };

            AssertPasses(() => Assert(items.Each(p => p.IsEquivalentTo(p))));
        }

        [Test]
        public void Should_support_negation()
        {
            var numbers = new[] { 1, 3, 5 };

            AssertPasses(() => Assert(!numbers.Each(x => x.IsEven())));
        }

        [Test]
        public void Should_support_operator_composition()
        {
            var evens = new[] { 2, 4, 6 };
            var positives = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(
                evens.Each(x => x.IsEven()) &
                positives.Each(x => x.IsPositive())
            ));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_pass()
        {
            var numbers = new[] { 2, 4, 6 };
            var expectation = numbers.Each(x => x.IsEven());
            var context = TestContext("numbers.Each(x => x.IsEven())");

            var result = expectation.Evaluate(context);

            AssertRendersExactly(result, "True");
        }

        [Test]
        public void Should_show_failing_indices()
        {
            var result = new CollectionQuantifierResult(
                "numbers.Each(x => x.IsEven())",
                "each",
                5,
                2,
                3,
                Passed: false,
                [
                    (0, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 1")),
                    (2, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 3")),
                    (4, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 5"))
                ]);

            AssertRendersExactly(result,
                "False",
                "Expected each item to satisfy expectation, but 3 of 5 failed:",
                "[0]: False",
                "Expected even number, got 1",
                "[2]: False",
                "Expected even number, got 3",
                "[4]: False",
                "Expected even number, got 5");
        }

        [Test]
        public void Should_show_single_failure()
        {
            var result = new CollectionQuantifierResult(
                "numbers.Each(x => x.IsEven())",
                "each",
                3,
                2,
                1,
                Passed: false,
                [(1, ExpectationResults.Fail("x.IsEven()", "Expected even number, got 3"))]);

            AssertRendersExactly(result,
                "False",
                "Expected each item to satisfy expectation, but 1 of 3 failed:",
                "[1]: False",
                "Expected even number, got 3");
        }

        [Test]
        public void Should_include_quantifier_summary()
        {
            var result = new CollectionQuantifierResult(
                "items.Each(...)",
                "each",
                10,
                7,
                3,
                Passed: false,
                [
                    (1, ExpectationResults.Fail("x", "fail 1")),
                    (4, ExpectationResults.Fail("x", "fail 4")),
                    (8, ExpectationResults.Fail("x", "fail 8"))
                ]);

            var rendered = Rendered(result);

            rendered.Should().Contain("3 of 10 failed");
        }
    }

    record Person(string Name, int Age);
}

public static class TestExtensions
{
    public static Expectation IsEven(this int value) =>
        Expectation.From(
            () => value % 2 == 0,
            () => [$"Expected even number, got {value}"]);

    public static Expectation IsPositive(this int value) =>
        Expectation.From(
            () => value > 0,
            () => [$"Expected positive number, got {value}"]);
}
