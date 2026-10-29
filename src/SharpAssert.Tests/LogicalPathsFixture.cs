using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class LogicalPathsFixture
{
    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public void Should_preserve_dynamic_AND_short_circuit(bool first, int expectedCalls)
    {
        dynamic left = first;
        var calls = 0;
        Func<bool> right = () => { calls++; return false; };

        Action assertion = () => Assert(left && right());

        assertion.Should().Throw<SharpAssertionException>();
        calls.Should().Be(expectedCalls);
    }

    [Test]
    public void Should_keep_dynamic_OR_short_circuit()
    {
        dynamic left = true;
        Func<bool> right = () => throw new InvalidOperationException();

        Assert(left || right());
    }

    [TestCase(false, new[] { 1 })]
    [TestCase(true, new[] { 1, 2 })]
    public async Task Should_preserve_awaited_AND_short_circuit(bool firstValue, int[] expectedCalls)
    {
        var calls = new List<int>();
        Func<Task<bool>> first = () => { calls.Add(1); return Task.FromResult(firstValue); };
        Func<Task<bool>> second = () => { calls.Add(2); return Task.FromResult(false); };

        Func<Task> assertion = async () => Assert(await first() && await second());

        await assertion.Should().ThrowAsync<SharpAssertionException>();
        calls.Should().Equal(expectedCalls);
    }

    [Test]
    public async Task Should_keep_awaited_OR_short_circuit()
    {
        Func<Task<bool>> first = () => Task.FromResult(true);
        Func<Task<bool>> second = () => throw new InvalidOperationException();

        Assert(await first() || await second());
    }
}
