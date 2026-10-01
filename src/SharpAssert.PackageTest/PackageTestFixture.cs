using FluentAssertions;
using static SharpAssert.Sharp;

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
    public void Should_compile_native_property_patterns_from_package()
    {
        object value = new Item(42);

        Assert(value is Item { Id: 42 });
    }

    [Test]
    public void Should_evaluate_all_independent_AND_checks_from_package()
    {
        var calls = 0;
        Func<bool> check = () => { calls++; return false; };

        Action assertion = () => Assert(check() && check());

        assertion.Should().Throw<SharpAssertionException>();
        calls.Should().Be(2);
    }

    [Test]
    public void Should_compile_nullable_selectors_from_package()
    {
        var actual = new Item(42);
        var expected = new Item(42, "different");

        Assert(actual.IsEquivalentTo(expected, config => config.Excluding(item => item.Name)));
    }

    [Test]
    public void Should_compare_DateTimeOffset_instants_from_package()
    {
        var actual = DateTimeOffset.UnixEpoch;
        var expected = actual.ToOffset(TimeSpan.FromHours(2));

        Assert(actual.IsEquivalentTo(expected));
    }

    [Test]
    public void Should_report_missing_inline_exceptions_from_package()
    {
        var calls = 0;
        Action operation = () => calls++;

        Action assertion = () => Assert(Throws<ArgumentException>(operation).Message == "expected");

        assertion.Should().Throw<SharpAssertionException>().Which.Message.Should().Contain("no exception was thrown");
        calls.Should().Be(1);
    }

    [Test]
    public void Should_work_with_null_values()
    {
        string? nullValue = null;
        string nonNullValue = "test";
        
        Assert(nonNullValue != null);
        
        var ex = Throws<SharpAssertionException>(() =>
            Assert(nullValue != null));
            
        ex.Message.Should().Contain("nullValue != null");
        ex.Message.Should().Contain("null");
    }

    record Item(int Id, string? Name = null);
}