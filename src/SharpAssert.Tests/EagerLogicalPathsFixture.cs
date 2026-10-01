using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class EagerLogicalPathsFixture
{
    [TestCase(false)]
    [TestCase(true)]
    public void Should_evaluate_dynamic_AND_operands_once(bool first)
    {
        dynamic left = first;
        var calls = 0;
        Func<bool> right = () => { calls++; return false; };

        Action assertion = () => Assert(left && right());

        assertion.Should().Throw<SharpAssertionException>();
        calls.Should().Be(1);
    }

    [Test]
    public void Should_keep_dynamic_OR_short_circuit()
    {
        dynamic left = true;
        Func<bool> right = () => throw new InvalidOperationException();

        Assert(left || right());
    }

    [Test]
    public async Task Should_evaluate_awaited_AND_operands_once_in_source_order()
    {
        var calls = new List<int>();
        Func<Task<bool>> first = () => { calls.Add(1); return Task.FromResult(false); };
        Func<Task<bool>> second = () => { calls.Add(2); return Task.FromResult(false); };

        Func<Task> assertion = async () => Assert(await first() && await second());

        await assertion.Should().ThrowAsync<SharpAssertionException>();
        calls.Should().Equal(1, 2);
    }

    [Test]
    public async Task Should_keep_awaited_OR_short_circuit()
    {
        Func<Task<bool>> first = () => Task.FromResult(true);
        Func<Task<bool>> second = () => throw new InvalidOperationException();

        Assert(await first() || await second());
    }
}
