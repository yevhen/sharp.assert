using System.Collections;
using SharpAssert.Features.CollectionComparison;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class CollectionComparisonFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_detect_first_mismatch()
        {
            var left = new List<int> { 1, 2, 3 };
            var right = new List<int> { 1, 2, 4 };

            var expected = BinaryComparison(
                "left == right",
                Equal,
                CollectionComparison(
                    left, right,
                    [1, 2, 3],
                    [1, 2, 4],
                    firstMismatch: Mismatch(2, 3, 4)));

            AssertFails(() => Assert(left == right), expected);
        }

        [Test]
        public void Should_detect_missing_elements()
        {
            var left = new List<int> { 1, 2 };
            var right = new List<int> { 1, 2, 3 };

            var expected = BinaryComparison(
                "left == right",
                Equal,
                CollectionComparison(
                    left, right,
                    [1, 2],
                    [1, 2, 3],
                    lengthDiff: LengthDiff(missing: [3])));

            AssertFails(() => Assert(left == right), expected);
        }

        [Test]
        public void Should_detect_extra_elements()
        {
            var left = new List<int> { 1, 2, 3 };
            var right = new List<int> { 1, 2 };

            var expected = BinaryComparison(
                "left == right",
                Equal,
                CollectionComparison(
                    left, right,
                    [1, 2, 3],
                    [1, 2],
                    lengthDiff: LengthDiff(extra: [3])));

            AssertFails(() => Assert(left == right), expected);
        }

        [Test]
        public void Should_handle_empty_collections()
        {
            var left = new List<int>();
            var right = new List<int> { 1 };

            var expected = BinaryComparison(
                "left == right",
                Equal,
                CollectionComparison(
                    left, right,
                    Array.Empty<object?>(),
                    [1],
                    lengthDiff: LengthDiff(missing: [1])));

            AssertFails(() => Assert(left == right), expected);
        }

        [Test]
        public void Should_pass_when_equal_via_sequence_equal()
        {
            var left = new List<int> { 1, 2, 3 };
            var right = new List<int> { 1, 2, 3 };

            AssertPasses(() => Assert(left.SequenceEqual(right)));
        }

        [Test]
        public void Should_pass_when_empty_collections_equal()
        {
            var left = new List<int>();
            var right = new List<int>();

            AssertPasses(() => Assert(left.SequenceEqual(right)));
        }

        [Test]
        public void Should_pass_with_different_collection_types()
        {
            var list = new List<int> { 1, 2, 3 };
            var array = new[] { 1, 2, 3 };

            AssertPasses(() => Assert(list.SequenceEqual(array)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_empty_collections()
        {
            var result = CollectionComparison(
                Array.Empty<object?>(),
                Array.Empty<object?>());

            AssertRendersExactly(result,
                "Left:  []",
                "Right: []");
        }

        [Test]
        public void Should_render_collection_previews()
        {
            var result = CollectionComparison(
                [1, 2, 3],
                [4, 5, 6]);

            AssertRendersExactly(result,
                "Left:  [1, 2, 3]",
                "Right: [4, 5, 6]");
        }

        [Test]
        public void Should_render_first_mismatch()
        {
            var mismatch = Mismatch(2, 3, 4);

            AssertRendersExactly(mismatch,
                "First difference at index 2: expected 3, got 4");
        }

        [Test]
        public void Should_render_missing_elements()
        {
            var delta = LengthDiff(missing: [3]);

            AssertRendersExactly(delta,
                "Missing elements: [3]");
        }

        [Test]
        public void Should_render_extra_elements()
        {
            var delta = LengthDiff(extra: [3]);

            AssertRendersExactly(delta,
                "Extra elements: [3]");
        }

        [Test]
        public void Should_render_both_missing_and_extra()
        {
            var delta = LengthDiff(
                missing: [4, 5],
                extra: [1, 2]);

            AssertRendersExactly(delta,
                "Extra elements: [1, 2]",
                "Missing elements: [4, 5]");
        }

        [Test]
        public void Should_compose_full_comparison_with_mismatch()
        {
            var mismatch = Mismatch(2, 3, 4);
            var result = CollectionComparison(
                [1, 2, 3],
                [1, 2, 4],
                firstMismatch: mismatch);

            AssertRendersExactly(result,
                "Left:  [1, 2, 3]",
                "Right: [1, 2, 4]",
                Rendered(mismatch));
        }

        [Test]
        public void Should_compose_with_length_diff()
        {
            var delta = LengthDiff(missing: [3]);
            var result = CollectionComparison(
                [1, 2],
                [1, 2, 3],
                lengthDiff: delta);

            AssertRendersExactly(result,
                "Left:  [1, 2]",
                "Right: [1, 2, 3]",
                Rendered(delta));
        }
    }

    static CollectionComparisonResult CollectionComparison(
        object? left,
        object? right,
        IReadOnlyList<object?>? leftPreview = null,
        IReadOnlyList<object?>? rightPreview = null,
        CollectionMismatch? firstMismatch = null,
        CollectionLengthDelta? lengthDiff = null)
    {
        leftPreview ??= MaterializeOrEmpty(left);
        rightPreview ??= MaterializeOrEmpty(right);

        return new CollectionComparisonResult(
            Operand(left),
            Operand(right),
            leftPreview,
            rightPreview,
            firstMismatch,
            lengthDiff);
    }

    // Simplified for formatting/composition tests (operands irrelevant)
    static CollectionComparisonResult CollectionComparison(
        IReadOnlyList<object?> leftPreview,
        IReadOnlyList<object?> rightPreview,
        CollectionMismatch? firstMismatch = null,
        CollectionLengthDelta? lengthDiff = null) =>
        new(
            Operand(Array.Empty<object>()),
            Operand(Array.Empty<object>()),
            leftPreview,
            rightPreview,
            firstMismatch,
            lengthDiff);

    static CollectionMismatch Mismatch(int index, object? leftVal, object? rightVal) =>
        new(index, leftVal, rightVal);

    static CollectionLengthDelta LengthDiff(
        IReadOnlyList<object?>? missing = null,
        IReadOnlyList<object?>? extra = null) =>
        new(missing, extra);

    static IReadOnlyList<object?> MaterializeOrEmpty(object? value) =>
        value switch
        {
            null => [],
            IEnumerable enumerable => enumerable.Cast<object?>().ToArray(),
            _ => []
        };
}
