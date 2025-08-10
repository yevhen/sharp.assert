using FluentAssertions;
using static Sharp;
using static NUnit.Framework.Assert;

namespace SharpAssert.PackageTest;

[TestFixture]
public class PackageTestFixture
{
    [Test]
    public void Should_rewrite_basic_assertions_in_package()
    {
        var x = 1;
        var y = 2;

        Assert(x == 1);
        Assert(x < y);
        Assert(x != y);
    }

    [Test]
    public void Should_provide_detailed_error_messages_when_assertions_fail()
    {
        var left = 5;
        var right = 10;

        var ex = Throws<SharpAssertionException>(() =>
            Assert(left >= right))!;

        ex.Message.Should().Contain("left >= right");
        ex.Message.Should().Contain("5");
        ex.Message.Should().Contain("10");
        ex.Message.Should().Contain("PackageTestFixture.cs");
    }

    [Test]
    public void Should_rewrite_complex_expressions()
    {
        var x = 1;
        var items = new[] { 1, 2, 3 };

        Assert(items.Length == 3);
        Assert(items.Contains(x));
    }

    [Test]
    public void Should_rewrite_boolean_expressions()
    {
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

    [Test]
    public void Should_work_with_null_values()
    {
        string? nullValue = null;
        string nonNullValue = "test";
        
        var ex = Throws<SharpAssertionException>(() =>
            Assert(nullValue != null))!;
            
        ex.Message.Should().Contain("nullValue != null");
        ex.Message.Should().Contain("null");
    }
}