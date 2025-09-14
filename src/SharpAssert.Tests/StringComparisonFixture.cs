namespace SharpAssert;

[TestFixture]
public class StringComparisonFixture : TestBase
{
    [Test]
    public void Should_show_inline_diff_for_strings()
    {
        var actual = "hello";
        var expected = "hallo";
        
        AssertExpressionThrows(
            () => actual == expected,
            "actual == expected",
            "StringComparisonFixture.cs",
            10,
            "*Assertion failed: actual == expected*" +
            "*Left:  \"hello\"*" +
            "*Right: \"hallo\"*" +
            "*Diff: h[-e][+a]llo*"); // Expected inline diff showing 'e' -> 'a' change
    }

    [Test]
    public void Should_handle_multiline_strings()
    {
        var actual = "line1\nline2\nline3";
        var expected = "line1\nMODIFIED\nline3";
        
        AssertExpressionThrows(
            () => actual == expected,
            "actual == expected",
            "StringComparisonFixture.cs",
            30,
            "*Assertion failed: actual == expected*" +
            "*Left:  \"line1*line2*line3\"*" +
            "*Right: \"line1*MODIFIED*line3\"*" +
            "*- line2*" +
            "*+ MODIFIED*");
    }

    [Test]
    public void Should_truncate_very_long_strings()
    {
        var longPart = new string('A', 1000);
        var actual = longPart + "X";
        var expected = longPart + "Y";
        
        AssertExpressionThrows(
            () => actual == expected,
            "actual == expected",
            "StringComparisonFixture.cs", 
            50,
            "*Assertion failed: actual == expected*" +
            "*Left:  \"*" +
            "*Right: \"*" +
            "*...*"); // Should show truncation indicator
    }

    [Test]
    public void Should_handle_null_strings()
    {
        string? nullString = null;
        var emptyString = "";
        
        AssertExpressionThrows(
            () => nullString == emptyString,
            "nullString == emptyString",
            "StringComparisonFixture.cs",
            68,
            "*Assertion failed: nullString == emptyString*" +
            "*Left:  null*" +
            "*Right: \"\"*"); // Should clearly distinguish null from empty
    }

    [Test]
    public void Should_pass_when_identical_strings_compared()
    {
        var str1 = "hello world";
        var str2 = "hello world";
        AssertExpressionPasses(() => str1 == str2);
    }

    [Test]
    public void Should_pass_when_both_strings_are_null()
    {
        string? str1 = null;
        string? str2 = null;
        AssertExpressionPasses(() => str1 == str2);
    }

    [Test]
    public void Should_pass_when_both_strings_are_empty()
    {
        var str1 = "";
        var str2 = string.Empty;
        AssertExpressionPasses(() => str1 == str2);
    }
}