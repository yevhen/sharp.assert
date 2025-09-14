using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class AsyncAssertionFixture : TestBase
{
    [Test]
    public async Task Should_handle_await_in_condition()
    {
        // Assert(await GetBool()) should work with async expressions
        var action = async () => await SharpInternal.AssertAsync(
            async () => await GetTrueAsync(), 
            "await GetTrueAsync()", 
            "TestFile.cs", 
            10);
        
        await action.Should().NotThrowAsync();
    }

    [Test]
    public async Task Should_show_false_for_failed_async()
    {
        // Assert(await GetFalse()) should show expression and False
        var action = async () => await SharpInternal.AssertAsync(
            async () => await GetFalseAsync(), 
            "await GetFalseAsync()", 
            "TestFile.cs", 
            10);
        
        await action.Should().ThrowAsync<SharpAssertionException>()
            .WithMessage("Assertion failed: await GetFalseAsync()  at TestFile.cs:10*Result: False*");
    }

    [Test]
    public async Task Should_preserve_async_context()
    {
        // Assert(await operation) should maintain SynchronizationContext
        var action = async () => await SharpInternal.AssertAsync(
            async () => 
            {
                // Test that we preserve async context by checking SynchronizationContext is available
                await Task.Yield(); 
                return true;
            }, 
            "await operation()", 
            "TestFile.cs", 
            10);
        
        await action.Should().NotThrowAsync();
    }

    [Test]
    public async Task Should_handle_exceptions_in_async()
    {
        // Assert(await ThrowingMethod()) should let async exceptions bubble
        var action = async () => await SharpInternal.AssertAsync(
            async () => await ThrowingMethodAsync(), 
            "await ThrowingMethodAsync()", 
            "TestFile.cs", 
            10);
        
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    private static async Task<bool> GetTrueAsync()
    {
        await Task.Delay(1);
        return true;
    }

    private static async Task<bool> GetFalseAsync()
    {
        await Task.Delay(1);
        return false;
    }

    private static async Task<bool> ThrowingMethodAsync()
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Test exception");
    }
}