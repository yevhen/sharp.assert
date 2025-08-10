using FluentAssertions;
using static NUnit.Framework.Assert;
using static Sharp;

namespace SharpAssert.IntegrationTest;

[TestFixture]
public class IntegrationTestFixture
{
    [Test]
    public void Should_rewrite_basic_assertions()
    {
        var x = 1;
        var y = 2;

        // These Assert calls should be rewritten to lambda form during build
        Assert(x == 1);
        Assert(x < y);
        Assert(x != y);
    }

    [Test]
    public void Should_intercept_assert_calls()
    {
        var left = 5;
        var right = 10;

        var ex = Throws<SharpAssertionException>(() =>
            Assert(left >= right))!;

        ex.Message.Should().Contain("left >= right");
        ex.Message.Should().Contain("*5*10*");
        ex.Message.Should().Contain("TestFile.cs");
    }

    [Test]
    public void Should_rewrite_complex_expressions()
    {
        var x = 1;
        
        // Test with more complex expressions
        var items = new[] { 1, 2, 3 };
        Assert(items.Length == 3);
        Assert(items.Contains(x));
    }

    [Test]
    public void Should_rewrite_boolean_expressions()
    {
        // Test with boolean expressions
        var isTrue = true;
        Assert(isTrue);
        Assert(!false);
    }
    
    [Test]
    public void Should_rewrite_string_operations()
    {
        var name = "test";
        var expected = "test";
        
        Assert(name == expected);
        Assert(name.Length > 0);
        Assert(!string.IsNullOrEmpty(name));
    }
}