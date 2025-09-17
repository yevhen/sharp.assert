using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class AsyncBinaryFixture : TestBase
{
    [Test]
    public async Task Should_show_both_async_values()
    {
        async Task<int> Left() { await Task.Yield(); return 42; }
        async Task<int> Right() { await Task.Yield(); return 24; }

        var action = async () => await SharpInternal.AssertAsyncBinary(
            async () => await Left(),
            async () => await Right(), 
            BinaryOp.Eq,
            "await Left() == await Right()",
            "AsyncBinaryFixture.cs",
            123);

        await action.Should().ThrowAsync<SharpAssertionException>()
            .Where(ex => ex.Message.Contains("42") && ex.Message.Contains("24"));
    }

    [Test]
    public async Task Should_handle_mixed_async_sync()
    {
        async Task<int> GetAsyncValue() { await Task.Yield(); return 42; }

        var action = async () => await SharpInternal.AssertAsyncBinary(
            async () => await GetAsyncValue(),
            () => Task.FromResult<object?>(24), 
            BinaryOp.Eq,
            "await GetAsyncValue() == 24",
            "AsyncBinaryFixture.cs",
            123);

        await action.Should().ThrowAsync<SharpAssertionException>()
            .Where(ex => ex.Message.Contains("42") && ex.Message.Contains("24"));
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

        var action = async () => await SharpInternal.AssertAsyncBinary(
            async () => await First(),
            async () => await Second(), 
            BinaryOp.Eq,
            "await First() == await Second()",
            "AsyncBinaryFixture.cs",
            123);

        await action.Should().ThrowAsync<SharpAssertionException>();
        
        evaluationOrder.Should().Equal("First", "Second");
    }

    [Test]
    public async Task Should_apply_diffs_to_async_strings()
    {
        async Task<string> GetString1() { await Task.Yield(); return "hello"; }
        async Task<string> GetString2() { await Task.Yield(); return "hallo"; }

        var action = async () => await SharpInternal.AssertAsyncBinary(
            async () => await GetString1(),
            async () => await GetString2(), 
            BinaryOp.Eq,
            "await GetString1() == await GetString2()",
            "AsyncBinaryFixture.cs",
            123);

        await action.Should().ThrowAsync<SharpAssertionException>()
            .Where(ex => ex.Message.Contains("h[-e][+a]llo"));
    }
}