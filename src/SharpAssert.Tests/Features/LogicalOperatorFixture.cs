using System.Linq.Expressions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class LogicalOperatorFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_handle_AND_failure()
        {
            var left = true;
            var right = false;
            
            var expected = Logical(
                "left && right",
                LogicalOperator.AndAlso,
                Value("left", true),
                Value("right", false),
                value: false,
                shortCircuited: false,
                nodeType: ExpressionType.AndAlso);

            AssertFails(() => Assert(left && right), expected);
        }

        [Test]
        public void Should_evaluate_both_operands_AND_when_left_fails()
        {
            var left = false;
            var right = false;

            var expected = Logical(
                "left && right",
                LogicalOperator.AndAlso,
                Value("left", false),
                Value("right", false),
                value: false,
                shortCircuited: false,
                nodeType: ExpressionType.AndAlso);

            AssertFails(() => Assert(left && right), expected);
        }

        [Test]
        public void Should_handle_OR_failure()
        {
            var left = false;
            var right = false;

            var expected = Logical(
                "left || right",
                LogicalOperator.OrElse,
                Value("left", false),
                Value("right", false),
                value: false,
                shortCircuited: false,
                nodeType: ExpressionType.OrElse);

            AssertFails(() => Assert(left || right), expected);
        }

        [Test]
        public void Should_handle_NOT_operator()
        {
            var operand = true;

            var expected = Unary(
                "!operand",
                UnaryOperator.Not,
                Value("operand", true),
                true,
                false);

            AssertFails(() => Assert(!operand), expected);
        }

        [Test]
        public void Should_handle_nested_logical_operators()
        {
            var x = 3;
            var y = 12;

            var expected = Logical(
                "x == 5 && y == 10",
                LogicalOperator.AndAlso,
                BinaryComparison("x == 5", Equal, Comparison(3, 5), false),
                BinaryComparison("y == 10", Equal, Comparison(12, 10), false),
                false,
                false,
                ExpressionType.AndAlso);

            AssertFails(() => Assert(x == 5 && y == 10), expected);
        }

        [Test]
        public void Should_pass_when_true()
        {
            AssertPasses(() => Assert(true || false));
            AssertPasses(() => Assert(true && true));
            AssertPasses(() => Assert(!false));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_AND_failure()
        {
            var result = Logical(
                "a && b",
                LogicalOperator.AndAlso,
                Value("a", true),
                Value("b", false),
                false,
                false,
                ExpressionType.AndAlso);

            AssertRendersExactly(result,
                "a && b",
                "Left: True",
                "Right: False",
                "&&: Right operand was false");
        }

        [Test]
        public void Should_render_AND_both_failed()
        {
            var result = Logical(
                "a && b",
                LogicalOperator.AndAlso,
                Value("a", false),
                Value("b", false),
                false,
                false,
                ExpressionType.AndAlso);

            AssertRendersExactly(result,
                "a && b",
                "Left: False",
                "Right: False",
                "&&: Both operands were false");
        }

        [Test]
        public void Should_render_OR_failure()
        {
            var result = Logical(
                "a || b",
                LogicalOperator.OrElse,
                Value("a", false),
                Value("b", false),
                false,
                false,
                ExpressionType.OrElse);

            AssertRendersExactly(result,
                "a || b",
                "Left: False",
                "Right: False",
                "||: Both operands were false");
        }

        [Test]
        public void Should_render_NOT_failure()
        {
            var result = Unary(
                "!a",
                UnaryOperator.Not,
                Value("a", true),
                true,
                false);

            AssertRendersExactly(result,
                "!a",
                "Operand: True",
                "!: Operand was True");
        }

        [Test]
        public void Should_render_nested_failure()
        {
            // (x == 5) && (y == 10) - both fail
            var result = Logical(
                "x == 5 && y == 10",
                LogicalOperator.AndAlso,
                BinaryComparison("x == 5", Equal, Comparison(3, 5), false),
                BinaryComparison("y == 10", Equal, Comparison(12, 10), false),
                false,
                false,
                ExpressionType.AndAlso);

            AssertRendersExactly(result,
                "x == 5 && y == 10",
                "Left: x == 5",
                "Left:  3",
                "Right: 5",
                "Right: y == 10",
                "Left:  12",
                "Right: 10",
                "&&: Both operands were false");
        }
    }

    static bool ThrowException() => throw new InvalidOperationException();

    static LogicalEvaluationResult Logical(string expr, LogicalOperator op, EvaluationResult left,
        EvaluationResult? right, bool value, bool shortCircuited, ExpressionType nodeType) =>
        new(expr, op, left, right, value, shortCircuited, nodeType);

    static UnaryEvaluationResult Unary(string expr, UnaryOperator op, EvaluationResult operand, object? opValue,
        bool value) =>
        new(expr, op, operand, opValue, value);

    static BinaryComparison.DefaultComparisonResult Comparison(object? left, object? right) =>
        new(Operand(left), Operand(right));
}
