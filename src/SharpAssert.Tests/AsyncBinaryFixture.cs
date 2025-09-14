using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class AsyncBinaryFixture : TestBase
{
    [Test]
    public async Task Should_show_both_async_values()
    {
        // Arrange
        async Task<int> Left() { await Task.Yield(); return 42; }
        async Task<int> Right() { await Task.Yield(); return 24; }

        // Act & Assert
        var action = async () => await SharpInternal.AssertAsyncBinary(
            async () => await Left(),
            async () => await Right(), 
            BinaryOp.Eq,
            "await Left() == await Right()",
            "AsyncBinaryFixture.cs",
            123);

        // Should show both values in error message
        await action.Should().ThrowAsync<SharpAssertionException>()
            .Where(ex => ex.Message.Contains("42") && ex.Message.Contains("24"));
    }

    [Test]
    public async Task Should_handle_mixed_async_sync()
    {
        // Arrange
        async Task<int> GetAsyncValue() { await Task.Yield(); return 42; }

        // Act & Assert - async left, sync right
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
        // Arrange
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

        // Act & Assert
        var action = async () => await SharpInternal.AssertAsyncBinary(
            async () => await First(),
            async () => await Second(), 
            BinaryOp.Eq,
            "await First() == await Second()",
            "AsyncBinaryFixture.cs",
            123);

        await action.Should().ThrowAsync<SharpAssertionException>();
        
        // Should evaluate left then right in source order
        evaluationOrder.Should().Equal("First", "Second");
    }

    [Test]
    public async Task Should_apply_diffs_to_async_strings()
    {
        // Arrange
        async Task<string> GetString1() { await Task.Yield(); return "hello"; }
        async Task<string> GetString2() { await Task.Yield(); return "hallo"; }

        // Act & Assert
        var action = async () => await SharpInternal.AssertAsyncBinary(
            async () => await GetString1(),
            async () => await GetString2(), 
            BinaryOp.Eq,
            "await GetString1() == await GetString2()",
            "AsyncBinaryFixture.cs",
            123);

        // Should show string diff in error message
        await action.Should().ThrowAsync<SharpAssertionException>()
            .Where(ex => ex.Message.Contains("h[-e][+a]llo"));
    }
}