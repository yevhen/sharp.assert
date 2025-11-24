using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class SequenceEqualFixture : TestBase
{
    [TestFixture]
    public class PositiveTestCases : TestBase
    {
        [Test]
        public void Should_pass_when_sequences_are_equal()
        {
            var seq1 = new List<int> { 1, 2, 3, 4, 5 };
            var seq2 = new List<int> { 1, 2, 3, 4, 5 };
            AssertDoesNotThrow(() => Assert(seq1.SequenceEqual(seq2)));
        }

        [Test]
        public void Should_pass_when_both_sequences_are_empty()
        {
            var seq1 = new List<string>();
            var seq2 = Array.Empty<string>();
            AssertDoesNotThrow(() => Assert(seq1.SequenceEqual(seq2)));
        }

        [Test]
        public void Should_pass_with_different_collection_types()
        {
            var list = new List<int> { 1, 2, 3 };
            var array = new[] { 1, 2, 3 };
            AssertDoesNotThrow(() => Assert(list.SequenceEqual(array)));
        }

        [Test]
        public void Should_pass_with_custom_comparer()
        {
            var seq1 = new[] { "Hello", "World" };
            var seq2 = new[] { "hello", "world" };
            var comparer = System.StringComparer.OrdinalIgnoreCase;
            AssertDoesNotThrow(() => Assert(seq1.SequenceEqual(seq2, comparer)));
        }

        [Test]
        public void Should_pass_with_single_element_sequences()
        {
            var seq1 = new[] { 42 };
            var seq2 = new[] { 42 };
            AssertDoesNotThrow(() => Assert(seq1.SequenceEqual(seq2)));
        }
    }

    [TestFixture]
    public class FailureFormatting : TestBase
    {
        [Test]
        public void Should_show_unified_diff_for_different_sequences()
        {
            var seq1 = new[] { 1, 2, 3 };
            var seq2 = new[] { 1, 2, 4 };

            AssertThrows(() => Assert(seq1.SequenceEqual(seq2)), "*SequenceEqual failed*unified diff*");
        }

        [Test]
        public void Should_handle_different_lengths()
        {
            var shortSeq = new[] { 1, 2, 3 };
            var longSeq = new[] { 1, 2, 3, 4, 5 };

            AssertThrows(() => Assert(shortSeq.SequenceEqual(longSeq)), "*SequenceEqual failed*length*");
        }

        [Test]
        public void Should_truncate_large_sequences()
        {
            var largeSeq1 = Enumerable.Range(1, 100).ToArray();
            var largeSeq2 = Enumerable.Range(1, 100).Select(x => x > 50 ? x + 1000 : x).ToArray();

            AssertThrows(() => Assert(largeSeq1.SequenceEqual(largeSeq2)), "*truncated*");
        }

        [Test]
        public void Should_work_with_custom_comparers()
        {
            var seq1 = new[] { "Hello", "World" };
            var seq2 = new[] { "hello", "DIFFERENT" };
            var comparer = System.StringComparer.OrdinalIgnoreCase;

            AssertThrows(() => Assert(seq1.SequenceEqual(seq2, comparer)), "*SequenceEqual failed*");
        }

        [Test]
        public void Should_handle_empty_vs_non_empty_sequences()
        {
            var empty = Array.Empty<int>();
            var nonEmpty = new[] { 1, 2, 3 };

            AssertThrows(() => Assert(empty.SequenceEqual(nonEmpty)), "*SequenceEqual failed*length*");
        }

        [Test]
        public void Should_handle_string_sequences()
        {
            var seq1 = new[] { "apple", "banana", "cherry" };
            var seq2 = new[] { "apple", "grape", "cherry" };

            AssertThrows(() => Assert(seq1.SequenceEqual(seq2)), "*SequenceEqual failed*banana*grape*");
        }
    }

    [TestFixture]
    public class StaticExtensionMethods : TestBase
    {
        [Test]
        public void Should_handle_static_SequenceEqual_syntax()
        {
            var seq1 = new[] { 1, 2, 3 };
            var seq2 = new[] { 1, 2, 4 };

            AssertThrows(() => Assert(seq1.SequenceEqual(seq2)), "*SequenceEqual failed*");
        }
    }
}