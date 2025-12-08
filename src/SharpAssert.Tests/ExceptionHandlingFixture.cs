using System.Collections;
using System.Linq.Expressions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class ExceptionHandlingFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_handle_comparer_exceptions_in_real_expressions()
        {
            var list1 = new ArrayList { 1, 2, 3 };
            var list2 = new ArrayList { 4, 5, 6 };

            // list1.Count < list2.Count -> 3 < 3 -> False
            var expected = BinaryComparison(
                "list1.Count < list2.Count",
                ExpressionType.LessThan,
                Comparison(3, 3));

            AssertFails(() => Assert(list1.Count < list2.Count), expected);
        }

        [Test]
        public void Should_pass_when_arraylist_count_comparison_succeeds()
        {
            var list1 = new ArrayList { 1, 2, 3 };
            var list2 = new ArrayList { 4, 5, 6, 7 };

            AssertPasses(() => Assert(list1.Count < list2.Count));
        }

        [Test]
        public void Should_pass_when_arraylist_equality_succeeds()
        {
            var list1 = new ArrayList { 1, 2, 3 };
            var list2 = new ArrayList { 1, 2, 3 };

            AssertPasses(() => Assert(list1.Count == list2.Count));
        }

        [Test]
        public void Should_pass_when_arraylist_greater_than_comparison_succeeds()
        {
            var list1 = new ArrayList { 1, 2, 3, 4, 5 };
            var list2 = new ArrayList { 1, 2 };

            AssertPasses(() => Assert(list1.Count > list2.Count));
        }

        [Test]
        public void Should_pass_when_arraylist_less_than_or_equal_comparison_succeeds()
        {
            var list1 = new ArrayList { 1, 2 };
            var list2 = new ArrayList { 1, 2 };

            AssertPasses(() => Assert(list1.Count <= list2.Count));
        }
    }

    static Features.BinaryComparison.DefaultComparisonResult Comparison(object? left, object? right) =>
        new(Operand(left), Operand(right));
}