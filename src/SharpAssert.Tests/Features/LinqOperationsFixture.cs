using SharpAssert.Core;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class LinqOperationsFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_handle_Contains_failure()
        {
            var items = new[] { 1, 2, 3 };
            
            var expected = Formatted("items.Contains(999)",
                "Contains failed: searched for 999 in [1, 2, 3]",
                "Count: 3");

            AssertFails(() => Assert(items.Contains(999)), expected);
        }

        [Test]
        public void Should_handle_Any_failure()
        {
            var items = new[] { 1, 2, 3 };
            
            var expected = Formatted("items.Any(x => x > 10)",
                "Any failed: no items matched x => (x > 10) in [1, 2, 3]");

            AssertFails(() => Assert(items.Any(x => x > 10)), expected);
        }

        [Test]
        public void Should_handle_All_failure()
        {
            var items = new[] { -1, 0, 1, 2 };
            
            var expected = Formatted("items.All(x => x > 0)",
                "All failed: items [-1, 0] did not match x => (x > 0)");

            AssertFails(() => Assert(items.All(x => x > 0)), expected);
        }

        [Test]
        public void Should_handle_empty_Any()
        {
            var empty = Array.Empty<int>();
            
            var expected = Formatted("empty.Any()",
                "Any failed: collection is empty");

            AssertFails(() => Assert(empty.Any()), expected);
        }

        [Test]
        public void Should_handle_static_syntax()
        {
            var items = new[] { 1, 2, 3 };
            var expected = Formatted("Enumerable.Contains(items, 999)",
                "Contains failed: searched for 999 in [1, 2, 3]",
                "Count: 3");

            AssertFails(() => Assert(Enumerable.Contains(items, 999)), expected);
        }

        [Test]
        public void Should_pass_when_Contains_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertPasses(() => Assert(items.Contains(2)));
        }

        [Test]
        public void Should_pass_when_Any_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertPasses(() => Assert(items.Any(x => x > 1)));
        }

        [Test]
        public void Should_pass_when_All_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertPasses(() => Assert(items.All(x => x > 0)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_Contains_failure()
        {
            var result = Formatted("items.Contains(999)",
                "Contains failed: searched for 999 in [1, 2, 3]",
                "Count: 3");

            AssertRendersExactly(result,
                "items.Contains(999)",
                "Contains failed: searched for 999 in [1, 2, 3]",
                "Count: 3");
        }

        [Test]
        public void Should_render_Any_failure()
        {
            var result = Formatted("items.Any(x => x > 10)",
                "Any failed: no items matched x => (x > 10) in [1, 2, 3]");

            AssertRendersExactly(result,
                "items.Any(x => x > 10)",
                "Any failed: no items matched x => (x > 10) in [1, 2, 3]");
        }

        [Test]
        public void Should_render_truncated_collection()
        {
            var result = Formatted("items.Contains(999)",
                "Contains failed: searched for 999 in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...]",
                "Count: 15");

            AssertRendersExactly(result,
                "items.Contains(999)",
                "Contains failed: searched for 999 in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...]",
                "Count: 15");
        }
    }

    static FormattedEvaluationResult Formatted(string expr, params string[] lines) => new(expr, false, lines);
}