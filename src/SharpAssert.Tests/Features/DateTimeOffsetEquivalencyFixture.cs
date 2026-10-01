using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class DateTimeOffsetEquivalencyFixture
{
    [TestCase("value")]
    [TestCase("nullable")]
    [TestCase("nested")]
    [TestCase("collection")]
    [TestCase("dictionary")]
    public void Should_compare_instants_by_default(string shape)
    {
        var instant = new DateTimeOffset(2025, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var actual = Wrap(shape, instant);
        var expected = Wrap(shape, instant.ToOffset(TimeSpan.FromHours(2)));

        Assert(actual.IsEquivalentTo(expected));
    }

    [TestCase("value")]
    [TestCase("nullable")]
    [TestCase("nested")]
    [TestCase("collection")]
    [TestCase("dictionary")]
    public void Should_reject_different_instants(string shape)
    {
        var instant = new DateTimeOffset(2025, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var actual = Wrap(shape, instant);
        var expected = Wrap(shape, instant.AddTicks(1));

        Action assertion = () => Assert(actual.IsEquivalentTo(expected));

        assertion.Should().Throw<SharpAssertionException>();
    }

    [TestCase("value")]
    [TestCase("nullable")]
    [TestCase("nested")]
    [TestCase("collection")]
    [TestCase("dictionary")]
    public void Should_allow_explicit_offset_comparison(string shape)
    {
        var instant = new DateTimeOffset(2025, 1, 2, 12, 0, 0, TimeSpan.Zero);
        var actual = Wrap(shape, instant);
        var expected = Wrap(shape, instant.ToOffset(TimeSpan.FromHours(2)));

        Action assertion = () => Assert(actual.IsEquivalentTo(expected,
            config => config.Using<DateTimeOffset>((left, right) => left.EqualsExact(right))));

        assertion.Should().Throw<SharpAssertionException>();
    }

    [Test]
    public void Should_pass_explicit_offset_comparison_for_identical_values()
    {
        var actual = new DateTimeOffset(2025, 1, 2, 12, 0, 0, TimeSpan.FromHours(2));

        Assert(actual.IsEquivalentTo(actual,
            config => config.Using<DateTimeOffset>((left, right) => left.EqualsExact(right))));
    }

    [Test]
    public void Should_compare_nullable_instants()
    {
        DateTimeOffset? actual = DateTimeOffset.UnixEpoch;
        DateTimeOffset? expected = DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromHours(-3));

        Assert(actual.IsEquivalentTo(expected));
    }

    [Test]
    public void Should_apply_explicit_equality_comparer_before_the_default()
    {
        var actual = DateTimeOffset.UnixEpoch;
        var expected = actual.ToOffset(TimeSpan.FromHours(2));
        var comparer = new ExactOffsetComparer();

        Action assertion = () => Assert(actual.IsEquivalentTo(expected, config => config.Using(comparer)));

        assertion.Should().Throw<SharpAssertionException>();
        Assert(actual.IsEquivalentTo(actual, config => config.Using(comparer)));
    }

    [Test]
    public void Should_keep_nullable_null_comparison()
    {
        DateTimeOffset? actual = null;
        DateTimeOffset? expected = null;

        Assert(actual.IsEquivalentTo(expected));
    }

    [Test]
    public void Should_reject_nullable_null_against_an_instant()
    {
        DateTimeOffset? actual = null;
        DateTimeOffset? expected = DateTimeOffset.UnixEpoch;

        Action assertion = () => Assert(actual.IsEquivalentTo(expected));

        assertion.Should().Throw<SharpAssertionException>();
    }

    static object Wrap(string shape, DateTimeOffset value) => shape switch
    {
        "value" => value,
        "nullable" => new Timestamp(value),
        "nested" => new { Child = new Timestamp(value) },
        "collection" => new DateTimeOffset?[] { value },
        "dictionary" => new Dictionary<string, DateTimeOffset?> { ["time"] = value },
        _ => throw new ArgumentException(nameof(shape))
    };

    record Timestamp(DateTimeOffset? Value);

    sealed class ExactOffsetComparer : IEqualityComparer<DateTimeOffset>
    {
        public bool Equals(DateTimeOffset left, DateTimeOffset right) => left.EqualsExact(right);
        public int GetHashCode(DateTimeOffset value) => HashCode.Combine(value.Ticks, value.Offset);
    }
}
