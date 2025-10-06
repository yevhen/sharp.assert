using static SharpAssert.Sharp;
using FluentAssertions;

namespace SharpAssert.IntegrationTests;

/// <summary>
/// Integration tests for async assertion functionality.
/// Tests the basic async assertion behavior through the MSBuild rewriter.
/// </summary>
[TestFixture]
public class AsyncIntegrationFixture
{
    [Test]
    public async Task Should_handle_basic_async_assertion_success()
    {
        var action = async () => Assert(await GetTrueAsync());
        
        await action.Should().NotThrowAsync();
    }
    
    [Test]
    public async Task Should_handle_basic_async_assertion_failure()
    {
        var action = async () => Assert(await GetFalseAsync());
        
        var exception = await action.Should().ThrowAsync<SharpAssertionException>();
        exception.Which.Message.Should().Contain("await GetFalseAsync()");
    }
    
    // Helper methods for testing async scenarios
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
}