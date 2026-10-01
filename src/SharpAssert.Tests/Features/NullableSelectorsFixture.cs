using System.Linq.Expressions;
using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class NullableSelectorsFixture
{
    [TestCase("Including", "Name")]
    [TestCase("Excluding", "Name")]
    [TestCase("Including", "Count")]
    [TestCase("Excluding", "Count")]
    [TestCase("Including", "Value")]
    [TestCase("Excluding", "Value")]
    public void Should_compile_nullable_selectors_without_warnings(string method, string member)
    {
        var source = $$"""
            using SharpAssert;
            using static SharpAssert.Sharp;
            public class Sample
            {
                public string? Name { get; set; }
                public int? Count { get; set; }
                public object? Value { get; set; }
                public static void Check()
                {
                    var actual = new Sample();
                    var expected = new Sample();
                    Assert(actual.IsEquivalentTo(expected, config => config.{{method}}(item => item.{{member}})));
                }
            }
            """;

        RewrittenSource.LoadCheck(source)();
    }

    [Test]
    public void Should_keep_existing_selector_signatures()
    {
        Expression<Func<Item, object>> selector = item => item.Id;
        var config = new EquivalencyConfig<Item>();
        var actual = new Item(1, null, null);
        var expected = new Item(1, "different", 2);

        config.Including(selector).Should().BeSameAs(config);
        config.Excluding(selector).Should().BeSameAs(config);
        typeof(EquivalencyConfig<Item>).GetMethod("Including", [typeof(Expression<Func<Item, object>>)])
            .Should().NotBeNull();
        typeof(EquivalencyConfig<Item>).GetMethod("Excluding", [typeof(Expression<Func<Item, object>>)])
            .Should().NotBeNull();
        Assert(actual.IsEquivalentTo(expected, each => each.Including(selector)));
    }

    [Test]
    public void Should_exclude_nullable_members()
    {
        var actual = new Item(1, null, null);
        var expected = new Item(1, "different", 2);

        Assert(actual.IsEquivalentTo(expected, config => config.Excluding(item => item.Name).Excluding(item => item.Count)));
    }

    [Test]
    public void Should_fail_when_included_nullable_member_differs()
    {
        var actual = new Item(1, null, null);
        var expected = new Item(1, "different", 2);

        Action assertion = () => Assert(actual.IsEquivalentTo(expected, config => config.Including(item => item.Count)));

        assertion.Should().Throw<SharpAssertionException>();
    }

    [TestCase("Including")]
    [TestCase("Excluding")]
    public void Should_reject_selectors_that_do_not_select_a_member(string method)
    {
        var config = new EquivalencyConfig<Item>();
        Action configure = method == "Including"
            ? () => config.Including(item => item.Id + 1)
            : () => config.Excluding(item => item.Id + 1);

        configure.Should().Throw<ArgumentException>();
    }

    record Item(int Id, string? Name, int? Count);
}
