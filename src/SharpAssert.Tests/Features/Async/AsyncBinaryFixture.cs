using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Async;

[TestFixture]
public class AsyncBinaryFixture : TestBase
{
    [Test]
    public async Task Should_show_both_async_values()
    {
        async Task<int> Left() { await Task.Yield(); return 42; }
        async Task<int> Right() { await Task.Yield(); return 24; }

        await AssertThrowsAsync(
            async () => Assert(await Left() == await Right()),
            "*42*24*");
    }

    [Test]
    public async Task Should_handle_mixed_async_sync()
    {
        async Task<int> GetAsyncValue() { await Task.Yield(); return 42; }

        await AssertThrowsAsync(
            async () => Assert(await GetAsyncValue() == 24),
            "*42*24*");
    }

    [Test]
    public async Task Should_evaluate_in_source_order()
    {
        var evaluationOrder = new List<string>();

        async Task<int> First()
        {
            await Task.Yield();
            evaluationOrder.Add("First");
            return 1;
        }

        async Task<int> Second()
        {
            await Task.Yield();
            evaluationOrder.Add("Second");
            return 2;
        }

        await AssertThrowsAsync(
            async () => Assert(await First() == await Second()),
            "*");

        evaluationOrder.Should().Equal("First", "Second");
    }

    [Test]
    public async Task Should_apply_diffs_to_async_strings()
    {
        async Task<string> GetString1() { await Task.Yield(); return "hello"; }
        async Task<string> GetString2() { await Task.Yield(); return "hallo"; }

        await AssertThrowsAsync(
            async () => Assert(await GetString1() == await GetString2()),
            "*h[-e][+a]llo*");
    }
}