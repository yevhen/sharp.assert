using SharpAssert.Core;
using SharpAssert.Features.Shared;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class ExpectationsFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_for_passing_expectation()
        {
            var expectation = new FixedExpectation(ExpectationResults.Boolean("ok", true));
            AssertPasses(() => Assert(expectation));
        }

        [Test]
        public void Should_fail_for_failing_expectation()
        {
            var expectation = new FixedExpectation(ExpectationResults.Fail("broken", "Broken"));
            var expected = ExpectationResults.Fail("broken", "Broken");
            AssertFails(() => Assert(expectation), expected);
        }

        [Test]
        public void Should_fail_NOT_when_operand_passes()
        {
            var operand = new FixedExpectation(ExpectationResults.Pass("operand"));

            var expected = new UnaryEvaluationResult(
                "operand.Not()",
                UnaryOperator.Not,
                ExpectationResults.Pass("operand"),
                true,
                false);

            AssertFails(() => Assert(operand.Not()), expected);
        }

        [Test]
        public void Should_pass_NOT_when_operand_fails()
        {
            var operand = new FixedExpectation(ExpectationResults.Fail("operand", "Broken"));
            AssertPasses(() => Assert(operand.Not()));
        }

        [Test]
        public void Should_support_operator_NOT()
        {
            var operand = new FixedExpectation(ExpectationResults.Pass("operand"));

            var expected = new UnaryEvaluationResult(
                "!operand",
                UnaryOperator.Not,
                ExpectationResults.Pass("operand"),
                true,
                false);

            AssertFails(() => Assert(!operand), expected);
        }

        [Test]
        public void Should_support_operator_AND()
        {
            var left = new ContextBooleanExpectation(true);
            var right = new ContextBooleanExpectation(false);

            var expected = new ComposedExpectationEvaluationResult(
                "left & right",
                "AND",
                ExpectationResults.Boolean("left", true),
                ExpectationResults.Boolean("right", false),
                false,
                false);

            AssertFails(() => Assert(left & right), expected);
        }

        [Test]
        public void Should_support_operator_OR()
        {
            var left = new ContextBooleanExpectation(false);
            var right = new ContextBooleanExpectation(false);

            var expected = new ComposedExpectationEvaluationResult(
                "left | right",
                "OR",
                ExpectationResults.Boolean("left", false),
                ExpectationResults.Boolean("right", false),
                false,
                false);

            AssertFails(() => Assert(left | right), expected);
        }

        [Test]
        public void Should_short_circuit_AND()
        {
            var left = new FixedExpectation(ExpectationResults.Fail("left", "Left failed"));
            var right = new ThrowingExpectation();
            var expectation = left.And(right);

            var expected = new ComposedExpectationEvaluationResult(
                "expectation",
                "AND",
                ExpectationResults.Fail("left", "Left failed"),
                null,
                false,
                true);

            AssertFails(() => Assert(expectation), expected);
        }

        [Test]
        public void Should_capture_expression_text_for_inline_AND()
        {
            var left = new FixedExpectation(ExpectationResults.Fail("left", "Left failed"));
            var right = new ThrowingExpectation();

            var expected = new ComposedExpectationEvaluationResult(
                "left.And(right)",
                "AND",
                ExpectationResults.Fail("left", "Left failed"),
                null,
                false,
                true);

            AssertFails(() => Assert(left.And(right)), expected);
        }

        [Test]
        public void Should_short_circuit_chained_AND()
        {
            var left = new FixedExpectation(ExpectationResults.Pass("left"));
            var middle = new FixedExpectation(ExpectationResults.Fail("middle", "Middle failed"));
            var right = new ThrowingExpectation();

            var expectation = left.And(middle).And(right);

            var expected = new ComposedExpectationEvaluationResult(
                "expectation",
                "AND",
                new ComposedExpectationEvaluationResult(
                    "expectation",
                    "AND",
                    ExpectationResults.Pass("left"),
                    ExpectationResults.Fail("middle", "Middle failed"),
                    false,
                    false),
                null,
                false,
                true);

            AssertFails(() => Assert(expectation), expected);
        }

        [Test]
        public void Should_pass_OR_when_left_passes()
        {
            var left = new FixedExpectation(ExpectationResults.Boolean("left", true));
            var right = new ThrowingExpectation();
            var expectation = left.Or(right);

            AssertPasses(() => Assert(expectation));
        }

        [Test]
        public void Should_fail_OR_when_both_fail()
        {
            var left = new FixedExpectation(ExpectationResults.Fail("left", "Left failed"));
            var right = new FixedExpectation(ExpectationResults.Fail("right", "Right failed"));
            var expectation = left.Or(right);

            var expected = new ComposedExpectationEvaluationResult(
                "expectation",
                "OR",
                ExpectationResults.Fail("left", "Left failed"),
                ExpectationResults.Fail("right", "Right failed"),
                false,
                false);

            AssertFails(() => Assert(expectation), expected);
        }

        [Test]
        public void Should_capture_expression_text_for_inline_OR()
        {
            var left = new FixedExpectation(ExpectationResults.Fail("left", "Left failed"));
            var right = new FixedExpectation(ExpectationResults.Fail("right", "Right failed"));

            var expected = new ComposedExpectationEvaluationResult(
                "left.Or(right)",
                "OR",
                ExpectationResults.Fail("left", "Left failed"),
                ExpectationResults.Fail("right", "Right failed"),
                false,
                false);

            AssertFails(() => Assert(left.Or(right)), expected);
        }

        [Test]
        public void Should_short_circuit_chained_OR()
        {
            var left = new FixedExpectation(ExpectationResults.Fail("left", "Left failed"));
            var middle = new FixedExpectation(ExpectationResults.Pass("middle"));
            var right = new ThrowingExpectation();

            var expectation = left.Or(middle).Or(right);

            AssertPasses(() => Assert(expectation));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_AND_failure()
        {
            var result = new ComposedExpectationEvaluationResult(
                "a.And(b)",
                "AND",
                ExpectationResults.Boolean("a", true),
                ExpectationResults.Fail("b", "B failed"),
                false,
                false);

            AssertRendersExactly(result,
                "a.And(b)",
                "Left: True",
                "Right: False",
                "B failed",
                "AND: Right operand was false");
        }

        [Test]
        public void Should_render_short_circuited_AND()
        {
            var result = new ComposedExpectationEvaluationResult(
                "a.And(b)",
                "AND",
                ExpectationResults.Fail("a", "A failed"),
                null,
                false,
                true);

            AssertRendersExactly(result,
                "a.And(b)",
                "Left: False",
                "A failed",
                "AND: Left operand was false");
        }

        [Test]
        public void Should_render_OR_failure()
        {
            var result = new ComposedExpectationEvaluationResult(
                "a.Or(b)",
                "OR",
                ExpectationResults.Fail("a", "A failed"),
                ExpectationResults.Fail("b", "B failed"),
                false,
                false);

            AssertRendersExactly(result,
                "a.Or(b)",
                "Left: False",
                "A failed",
                "Right: False",
                "B failed",
                "OR: Both operands were false");
        }

        [Test]
        public void Should_render_NOT_failure()
        {
            var result = new UnaryEvaluationResult(
                "a.Not()",
                UnaryOperator.Not,
                ExpectationResults.Boolean("a", true),
                true,
                false);

            AssertRendersExactly(result,
                "a.Not()",
                "Operand: True",
                "!: Operand was True");
        }
    }

    sealed class FixedExpectation(EvaluationResult result) : Expectation
    {
        public override EvaluationResult Evaluate(ExpectationContext context) => result;
    }

    sealed class ContextBooleanExpectation(bool value) : Expectation
    {
        public override EvaluationResult Evaluate(ExpectationContext context) =>
            ExpectationResults.Boolean(context.Expression, value);
    }

    sealed class ThrowingExpectation : Expectation
    {
        public override EvaluationResult Evaluate(ExpectationContext context) =>
            throw new InvalidOperationException("Should not be evaluated");
    }
}
