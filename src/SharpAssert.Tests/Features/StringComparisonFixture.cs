using DiffPlex.DiffBuilder.Model;
using SharpAssert.Features.StringComparison;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class StringComparisonFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_show_inline_diff_for_strings()
        {
            var actual = "hello";
            var expected = "hallo";

            var diff = InlineDiff(
                Segment(StringDiffOperation.Unchanged, "h"),
                Segment(StringDiffOperation.Deleted, "e"),
                Segment(StringDiffOperation.Inserted, "a"),
                Segment(StringDiffOperation.Unchanged, "llo"));

            var comparison = StringComparison(actual, expected, diff);
            var result = BinaryComparison("actual == expected", Equal, comparison);

            AssertFails(() => Assert(actual == expected), result);
        }

        [Test]
        public void Should_handle_multiline_strings()
        {
            var actual = "line1\nline2\nline3";
            var expected = "line1\nMODIFIED\nline3";

            var diff = MultilineDiff(
                DiffLine(ChangeType.Unchanged, "line1"),
                DiffLine(ChangeType.Deleted, "line2"),
                DiffLine(ChangeType.Inserted, "MODIFIED"),
                DiffLine(ChangeType.Unchanged, "line3"));

            var comparison = StringComparison(actual, expected, diff);
            var result = BinaryComparison("actual == expected", Equal, comparison);

            AssertFails(() => Assert(actual == expected), result);
        }

        [Test]
        public void Should_handle_null_strings()
        {
            string? nullString = null;
            var emptyString = "";

            var diff = InlineDiff(
                Segment(StringDiffOperation.Deleted, "null"),
                Segment(StringDiffOperation.Inserted, "\"\""));

            var comparison = StringComparison(nullString, emptyString, diff);
            var result = BinaryComparison("nullString == emptyString", Equal, comparison);

            AssertFails(() => Assert(nullString == emptyString), result);
        }

        [Test]
        public void Should_pass_when_identical_strings_compared()
        {
            var str1 = "hello world";
            var str2 = "hello world";
            AssertPasses(() => Assert(str1 == str2));
        }

        [Test]
        public void Should_pass_when_both_strings_are_null()
        {
            string? str1 = null;
            string? str2 = null;
            AssertPasses(() => Assert(str1 == str2));
        }

        [Test]
        public void Should_pass_when_both_strings_are_empty()
        {
            var str1 = "";
            var str2 = string.Empty;
            AssertPasses(() => Assert(str1 == str2));
        }

        [Test]
        public void Should_truncate_very_long_strings_in_diff()
        {
            var longPart = new string('A', 1000);
            var actual = longPart + "X";
            var expected = longPart + "Y";
            
            var diff = InlineDiff(
                Segment(StringDiffOperation.Unchanged, longPart + "..."));

            var comparison = StringComparison(actual, expected, diff);
            var result = BinaryComparison("actual == expected", Equal, comparison);

            AssertFails(() => Assert(actual == expected), result);
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_inline_diff()
        {
            var diff = InlineDiff(
                Segment(StringDiffOperation.Unchanged, "h"),
                Segment(StringDiffOperation.Deleted, "e"),
                Segment(StringDiffOperation.Inserted, "a"),
                Segment(StringDiffOperation.Unchanged, "llo"));

            var result = StringComparison("hello", "hallo", diff);

            AssertRendersExactly(result,
                "Left:  \"hello\"",
                "Right: \"hallo\"",
                "Diff: h[-e][+a]llo");
        }

        [Test]
        public void Should_render_multiline_diff()
        {
            var diff = MultilineDiff(
                DiffLine(ChangeType.Unchanged, "line1"),
                DiffLine(ChangeType.Deleted, "line2"),
                DiffLine(ChangeType.Inserted, "MODIFIED"),
                DiffLine(ChangeType.Unchanged, "line3"));

            var result = StringComparison("line1\nline2\nline3", "line1\nMODIFIED\nline3", diff);

            AssertRendersExactly(result,
                "Left:",
                "line1",
                "line2",
                "line3",
                "Right:",
                "line1",
                "MODIFIED",
                "line3",
                "Diff:",
                "  line1",
                "- line2",
                "+ MODIFIED",
                "  line3");
        }

        [Test]
        public void Should_render_nulls_with_diff()
        {
            var diff = InlineDiff(
                Segment(StringDiffOperation.Deleted, "null"),
                Segment(StringDiffOperation.Inserted, "\"\""));

            var result = StringComparison(null, "", diff);

            AssertRendersExactly(result,
                "Left:  null",
                "Right: \"\"",
                "Diff: [-null][+\"\"]");
        }
    }

    static StringComparisonResult StringComparison(string? left, string? right, StringDiff? diff) =>
        new(
            Operand(left, typeof(string)),
            Operand(right, typeof(string)),
            left,
            right,
            diff!); 

    static InlineStringDiff InlineDiff(params DiffSegment[] segments) => new(segments);

    static MultilineStringDiff MultilineDiff(params TextDiffLine[] lines) => new(lines);

    static DiffSegment Segment(StringDiffOperation op, string text) => new(op, text);

    static TextDiffLine DiffLine(ChangeType type, string text) => new(type, text);
}
