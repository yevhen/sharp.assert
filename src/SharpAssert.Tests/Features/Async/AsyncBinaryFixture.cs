using static SharpAssert.Sharp;
using FluentAssertions;

namespace SharpAssert.Features.Async;

[TestFixture]
public class AsyncBinaryFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public async Task Should_handle_async_binary_comparison()
        {
            async Task<int> Left() { await Task.Yield(); return 42; }
            async Task<int> Right() { await Task.Yield(); return 24; }

            var expected = BinaryComparison(
                "await Left() == await Right()",
                Equal,
                Comparison(42, 24));

            await AssertFailsAsync(async () => Assert(await Left() == await Right()), expected);
        }

        [Test]
        public async Task Should_handle_mixed_async_sync()
        {
            async Task<int> GetAsyncValue() { await Task.Yield(); return 42; }

            var expected = BinaryComparison(
                "await GetAsyncValue() == 24",
                Equal,
                Comparison(42, 24));

            await AssertFailsAsync(async () => Assert(await GetAsyncValue() == 24), expected);
        }

        [Test]
        public async Task Should_compare_different_number_types_by_value()
        {
            async Task<long> NextId() { await Task.Yield(); return 101L; }

            Assert(await NextId() == 101);
        }

        [Test]
        public async Task Should_order_different_number_types_by_value()
        {
            async Task<long> NextId() { await Task.Yield(); return 101L; }

            Assert(await NextId() > 100);
        }

        [Test]
        public async Task Should_compare_objects_with_their_equals()
        {
            async Task<Point> Origin() { await Task.Yield(); return new Point(0, 0); }

            Assert(await Origin() == new Point(0, 0));
        }

        [Test]
        public async Task Should_evaluate_in_source_order()
        {
            var evaluationOrder = new List<string>();

            async Task<int> First()
            {
                await Task.Yield();
                evaluationOrder.Add("First");
                return 1;
            }

            async Task<int> Second()
            {
                await Task.Yield();
                evaluationOrder.Add("Second");
                return 2;
            }

            var expected = BinaryComparison(
                "await First() == await Second()",
                Equal,
                Comparison(1, 2));

            await AssertFailsAsync(async () => Assert(await First() == await Second()), expected);

            evaluationOrder.Should().Equal("First", "Second");
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_async_comparison()
        {
            var result = BinaryComparison(
                "await Left() == await Right()",
                Equal,
                Comparison(42, 24));

            AssertRendersExactly(result,
                "await Left() == await Right()",
                "Left:  42",
                "Right: 24");
        }
    }

    static BinaryComparison.DefaultComparisonResult Comparison(object? left, object? right) =>
        new(Operand(left), Operand(right));

    class Point(int x, int y)
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        public override bool Equals(object? other) => other is Point point && point.X == X && point.Y == Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
