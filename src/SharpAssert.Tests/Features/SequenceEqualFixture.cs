using SharpAssert.Features.SequenceEqual;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class SequenceEqualFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_detect_sequence_differences()
        {
            var seq1 = new[] { 1, 2, 3 };
            var seq2 = new[] { 1, 2, 4 };

            var expected = BinaryComparison("seq1.SequenceEqual(seq2)", Equal,
                SequenceEqual(seq1, seq2, false,
                    diffs: [
                        DiffLine(SequenceDiffOperation.Context, 0, 1),
                        DiffLine(SequenceDiffOperation.Context, 1, 2),
                        DiffLine(SequenceDiffOperation.Removed, 2, 3),
                        DiffLine(SequenceDiffOperation.Added, 2, 4)
                    ]));

            AssertFails(() => Assert(seq1.SequenceEqual(seq2)), expected);
        }

        [Test]
        public void Should_detect_length_mismatch()
        {
            var shortSeq = new[] { 1, 2, 3 };
            var longSeq = new[] { 1, 2, 3, 4, 5 };

            var expected = BinaryComparison("shortSeq.SequenceEqual(longSeq)", Equal,
                SequenceEqual(shortSeq, longSeq, false,
                    lengthMismatch: LengthMismatch(expected: 5, actual: 3,
                        [1, 2, 3],
                        [1, 2, 3, 4, 5]
                    )));

            AssertFails(() => Assert(shortSeq.SequenceEqual(longSeq)), expected);
        }

        [Test]
        public void Should_handle_custom_comparer_failures()
        {
            var seq1 = new[] { "Hello", "World" };
            var seq2 = new[] { "hello", "DIFFERENT" };
            var comparer = StringComparer.OrdinalIgnoreCase;

            var expected = BinaryComparison("seq1.SequenceEqual(seq2, comparer)", Equal,
                SequenceEqual(seq1, seq2, true,
                    diffs: [
                        DiffLine(SequenceDiffOperation.Removed, 0, "Hello"),
                        DiffLine(SequenceDiffOperation.Removed, 1, "World"),
                        DiffLine(SequenceDiffOperation.Added, 0, "hello"),
                        DiffLine(SequenceDiffOperation.Added, 1, "DIFFERENT")
                    ]));

            AssertFails(() => Assert(seq1.SequenceEqual(seq2, comparer)), expected);
        }

        [Test]
        public void Should_pass_when_equal()
        {
            var seq1 = new List<int> { 1, 2, 3 };
            var seq2 = new List<int> { 1, 2, 3 };
            AssertPasses(() => Assert(seq1.SequenceEqual(seq2)));
        }

        [Test]
        public void Should_pass_with_static_syntax()
        {
            var seq1 = new[] { 1, 2, 3 };
            var seq2 = new[] { 1, 2, 3 };
            AssertPasses(() => Assert(Enumerable.SequenceEqual(seq1, seq2)));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_unified_diff()
        {
            var result = SequenceEqual(
                new[] { 1, 2, 3 }, 
                new[] { 1, 2, 4 }, 
                false,
                diffs: [
                    DiffLine(SequenceDiffOperation.Context, 0, 1),
                    DiffLine(SequenceDiffOperation.Removed, 2, 3),
                    DiffLine(SequenceDiffOperation.Added, 2, 4)
                ]);

            AssertRendersExactly(result,
                "SequenceEqual failed: sequences differ",
                "Unified diff:",
                "  [0] = 1",
                "- [2] = 3",
                "+ [2] = 4");
        }

        [Test]
        public void Should_render_length_mismatch()
        {
            var result = SequenceEqual(
                new[] { 1 }, 
                new[] { 1, 2 }, 
                false,
                lengthMismatch: LengthMismatch(2, 1, [1], [1, 2]));

            AssertRendersExactly(result,
                "SequenceEqual failed: length mismatch",
                "Expected length: 2",
                "Actual length:   1",
                "First:  [1]",
                "Second: [1, 2]");
        }

        [Test]
        public void Should_render_custom_comparer_notice()
        {
            var result = SequenceEqual(
                new[] { "A" }, 
                new[] { "B" }, 
                true, // HasComparer
                diffs: [
                    DiffLine(SequenceDiffOperation.Removed, 0, "A"),
                    DiffLine(SequenceDiffOperation.Added, 0, "B")
                ]);

            AssertRendersExactly(result,
                "SequenceEqual failed: sequences differ",
                "(using custom comparer)",
                "Unified diff:",
                "- [0] = \"A\"",
                "+ [0] = \"B\"");
        }

        [Test]
        public void Should_render_truncated_diff()
        {
            var result = SequenceEqual(
                new object(), 
                new object(), 
                false,
                diffs: [ DiffLine(SequenceDiffOperation.Context, 0, 1) ],
                truncated: true);

            AssertRendersExactly(result,
                "SequenceEqual failed: sequences differ",
                "Unified diff:",
                "  [0] = 1",
                "... (diff truncated)");
        }
    }

    static SequenceEqualComparisonResult SequenceEqual(
        object? left, 
        object? right, 
        bool hasComparer,
        SequenceLengthMismatch? lengthMismatch = null,
        SequenceDiffLine[]? diffs = null,
        bool truncated = false) =>
        new(
            Operand(left),
            Operand(right),
            hasComparer,
            lengthMismatch,
            diffs,
            truncated);

    static SequenceLengthMismatch LengthMismatch(int expected, int actual, object?[] first, object?[] second) =>
        new(expected, actual, first, second);

    static SequenceDiffLine DiffLine(SequenceDiffOperation op, int index, object? value) => new(op, index, value);
}
