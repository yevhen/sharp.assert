using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class AsyncAssertionFixture : TestBase
{
    [Test]
    public async Task Should_handle_await_in_condition()
    {
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
        var action = async () => await SharpInternal.AssertAsync(
            async () => 
            {
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
        var action = async () => await SharpInternal.AssertAsync(
            async () => await ThrowingMethodAsync(), 
            "await ThrowingMethodAsync()", 
            "TestFile.cs", 
            10);
        
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    static async Task<bool> GetTrueAsync()
    {
        await Task.Delay(1);
        return true;
    }

    static async Task<bool> GetFalseAsync()
    {
        await Task.Delay(1);
        return false;
    }

    static async Task<bool> ThrowingMethodAsync()
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Test exception");
    }
}