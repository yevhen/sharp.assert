using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class StringComparisonFixture : TestBase
{
    [Test]
    public void Should_show_inline_diff_for_strings()
    {
        var actual = "hello";
        var expected = "hallo";

        AssertThrows(
            () => Assert(actual == expected),
            "*Assertion failed: actual == expected*" +
            "*Left:  \"hello\"*" +
            "*Right: \"hallo\"*" +
            "*Diff: h[-e][+a]llo*");
    }

    [Test]
    public void Should_handle_multiline_strings()
    {
        var actual = "line1\nline2\nline3";
        var expected = "line1\nMODIFIED\nline3";

        AssertThrows(
            () => Assert(actual == expected),
            "*Assertion failed: actual == expected*" +
            "*Left:*" +
            "*line1*" +
            "*line2*" +
            "*line3*" +
            "*Right:*" +
            "*line1*" +
            "*MODIFIED*" +
            "*line3*" +
            "*Diff:*" +
            "*line1*" +
            "*- line2*" +
            "*+ MODIFIED*" +
            "*line3*");
    }

    [Test]
    public void Should_truncate_very_long_strings()
    {
        var longPart = new string('A', 1000);
        var actual = longPart + "X";
        var expected = longPart + "Y";

        AssertThrows(
            () => Assert(actual == expected),
            "*Assertion failed: actual == expected*" +
            "*Left:  \"*" +
            "*Right: \"*" +
            "*...*");
    }

    [Test]
    public void Should_handle_null_strings()
    {
        string? nullString = null;
        var emptyString = "";

        AssertThrows(
            () => Assert(nullString == emptyString),
            "*Assertion failed: nullString == emptyString*" +
            "*Left:  null*" +
            "*Right: \"\"*");
    }

    [Test]
    public void Should_pass_when_identical_strings_compared()
    {
        var str1 = "hello world";
        var str2 = "hello world";
        AssertDoesNotThrow(() => Assert(str1 == str2));
    }

    [Test]
    public void Should_pass_when_both_strings_are_null()
    {
        string? str1 = null;
        string? str2 = null;
        AssertDoesNotThrow(() => Assert(str1 == str2));
    }

    [Test]
    public void Should_pass_when_both_strings_are_empty()
    {
        var str1 = "";
        var str2 = string.Empty;
        AssertDoesNotThrow(() => Assert(str1 == str2));
    }
}