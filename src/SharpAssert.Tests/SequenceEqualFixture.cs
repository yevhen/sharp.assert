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
            AssertExpressionPasses(() => seq1.SequenceEqual(seq2));
        }

        [Test]
        public void Should_pass_when_both_sequences_are_empty()
        {
            var seq1 = new List<string>();
            var seq2 = Array.Empty<string>();
            AssertExpressionPasses(() => seq1.SequenceEqual(seq2));
        }

        [Test]
        public void Should_pass_with_different_collection_types()
        {
            var list = new List<int> { 1, 2, 3 };
            var array = new[] { 1, 2, 3 };
            AssertExpressionPasses(() => list.SequenceEqual(array));
        }

        [Test]
        public void Should_pass_with_custom_comparer()
        {
            var seq1 = new[] { "Hello", "World" };
            var seq2 = new[] { "hello", "world" };
            var comparer = StringComparer.OrdinalIgnoreCase;
            AssertExpressionPasses(() => seq1.SequenceEqual(seq2, comparer));
        }

        [Test]
        public void Should_pass_with_single_element_sequences()
        {
            var seq1 = new[] { 42 };
            var seq2 = new[] { 42 };
            AssertExpressionPasses(() => seq1.SequenceEqual(seq2));
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
            
            AssertExpressionThrows(
                () => seq1.SequenceEqual(seq2),
                "seq1.SequenceEqual(seq2)",
                "SequenceEqualFixture.cs",
                42,
                "*SequenceEqual failed*unified diff*");
        }

        [Test]
        public void Should_handle_different_lengths()
        {
            var shortSeq = new[] { 1, 2, 3 };
            var longSeq = new[] { 1, 2, 3, 4, 5 };
            
            AssertExpressionThrows(
                () => shortSeq.SequenceEqual(longSeq),
                "shortSeq.SequenceEqual(longSeq)",
                "SequenceEqualFixture.cs",
                42,
                "*SequenceEqual failed*length*");
        }

        [Test]
        public void Should_truncate_large_sequences()
        {
            var largeSeq1 = Enumerable.Range(1, 100).ToArray();
            var largeSeq2 = Enumerable.Range(1, 100).Select(x => x > 50 ? x + 1000 : x).ToArray(); // Many differences
            
            AssertExpressionThrows(
                () => largeSeq1.SequenceEqual(largeSeq2),
                "largeSeq1.SequenceEqual(largeSeq2)",
                "SequenceEqualFixture.cs",
                42,
                "*truncated*");
        }

        [Test]
        public void Should_work_with_custom_comparers()
        {
            var seq1 = new[] { "Hello", "World" };
            var seq2 = new[] { "hello", "DIFFERENT" };
            var comparer = StringComparer.OrdinalIgnoreCase;
            
            AssertExpressionThrows(
                () => seq1.SequenceEqual(seq2, comparer),
                "seq1.SequenceEqual(seq2, comparer)",
                "SequenceEqualFixture.cs",
                42,
                "*SequenceEqual failed*");
        }

        [Test]
        public void Should_handle_empty_vs_non_empty_sequences()
        {
            var empty = Array.Empty<int>();
            var nonEmpty = new[] { 1, 2, 3 };
            
            AssertExpressionThrows(
                () => empty.SequenceEqual(nonEmpty),
                "empty.SequenceEqual(nonEmpty)",
                "SequenceEqualFixture.cs",
                42,
                "*SequenceEqual failed*length*");
        }

        [Test]
        public void Should_handle_string_sequences()
        {
            var seq1 = new[] { "apple", "banana", "cherry" };
            var seq2 = new[] { "apple", "grape", "cherry" };
            
            AssertExpressionThrows(
                () => seq1.SequenceEqual(seq2),
                "seq1.SequenceEqual(seq2)",
                "SequenceEqualFixture.cs",
                42,
                "*SequenceEqual failed*banana*grape*");
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
            
            AssertExpressionThrows(
                () => seq1.SequenceEqual(seq2),
                "Enumerable.SequenceEqual(seq1, seq2)",
                "SequenceEqualFixture.cs",
                42,
                "*SequenceEqual failed*");
        }
    }
}