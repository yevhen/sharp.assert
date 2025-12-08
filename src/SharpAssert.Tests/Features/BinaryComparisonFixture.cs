using System.Linq.Expressions;
using SharpAssert.Features.BinaryComparison;
using static SharpAssert.Sharp;
using FluentAssertions;

namespace SharpAssert.Features;

[TestFixture]
public class BinaryComparisonFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        int callCount;

        int GetValue()
        {
            callCount++; 
            return callCount * 10; 
        }

        [Test]
        public void Should_handle_equality()
        {
            var left = 42;
            var right = 24;
            var expected = BinaryComparison("left == right", Equal, Comparison(left, right));

            AssertFails(() => Assert(left == right), expected);
        }

        [Test]
        public void Should_handle_inequality()
        {
            var left = 5;
            var right = 5;
            var expected = BinaryComparison("left != right", NotEqual, Comparison(left, right));

            AssertFails(() => Assert(left != right), expected);
        }

        [Test]
        public void Should_handle_less_than()
        {
            var left = 10;
            var right = 5;
            var expected = BinaryComparison("left < right", ExpressionType.LessThan, Comparison(left, right));

            AssertFails(() => Assert(left < right), expected);
        }

        [Test]
        public void Should_handle_less_than_or_equal()
        {
            var left = 10;
            var right = 5;
            var expected = BinaryComparison("left <= right", ExpressionType.LessThanOrEqual, Comparison(left, right));

            AssertFails(() => Assert(left <= right), expected);
        }

        [Test]
        public void Should_handle_greater_than()
        {
            var left = 5;
            var right = 10;
            var expected = BinaryComparison("left > right", ExpressionType.GreaterThan, Comparison(left, right));

            AssertFails(() => Assert(left > right), expected);
        }

        [Test]
        public void Should_handle_greater_than_or_equal()
        {
            var left = 5;
            var right = 10;
            var expected = BinaryComparison("left >= right", ExpressionType.GreaterThanOrEqual, Comparison(left, right));

            AssertFails(() => Assert(left >= right), expected);
        }

        [Test]
        public void Should_evaluate_operands_once()
        {
            callCount = 0;

            // 10 == 20 -> false
            var expected = BinaryComparison(
                "GetValue() == GetValue()",
                Equal,
                Comparison(10, 20));

            AssertFails(() => Assert(GetValue() == GetValue()), expected);
            callCount.Should().Be(2);
        }

        [Test]
        public void Should_handle_incompatible_types()
        {
            var str = "hello";
            var num = 42;
            var expected = BinaryComparison(
                "str.Length > num",
                ExpressionType.GreaterThan,
                Comparison(5, 42));

            AssertFails(() => Assert(str.Length > num), expected);
        }

        [Test]
        public void Should_pass_when_true()
        {
            AssertPasses(() => Assert(5 == 5));
            AssertPasses(() => Assert(5 < 10));
            AssertPasses(() => Assert("test" == "test"));
        }

        [Test]
        public void Should_capture_complex_expressions()
        {
            var x = 2;
            var y = 3;
            var z = 5;
            var expected = BinaryComparison(
                "x + y * z > 100",
                ExpressionType.GreaterThan,
                Comparison(17, 100)); // 2 + 15 = 17

            AssertFails(() => Assert(x + y * z > 100), expected);
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_simple_comparison()
        {
            var result = Comparison(42, 24);
            AssertRendersExactly(result,
                "Left:  42",
                "Right: 24");
        }

        [Test]
        public void Should_render_strings()
        {
            var result = Comparison("foo", "bar");
            // Default binary comparison uses ValueFormatter which quotes strings
            AssertRendersExactly(result,
                "Left:  \"foo\"",
                "Right: \"bar\"");
        }

        [Test]
        public void Should_render_nulls()
        {
            var result = Comparison(null, null);
            AssertRendersExactly(result,
                "Left:  null",
                "Right: null");
        }
        
        [Test]
        public void Should_render_complex_types()
        {
             // ValueFormatter calls ToString()
             var obj = new { Id = 1 };
             var result = Comparison(obj, obj);
             AssertRendersExactly(result,
                 $"Left:  {obj}",
                 $"Right: {obj}");
        }
    }

    static DefaultComparisonResult Comparison(object? left, object? right) => new(Operand(left), Operand(right));
}