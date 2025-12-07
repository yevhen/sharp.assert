using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Async;

[TestFixture]
public class AsyncAssertionFixture : TestBase
{
    [Test]
    public async Task Should_handle_await_in_condition()
    {
        await AssertDoesNotThrowAsync(async () => Assert(await GetTrueAsync()));
    }

    [Test]
    public async Task Should_show_false_for_failed_async()
    {
        await AssertThrowsAsync(
            async () => Assert(await GetFalseAsync()),
            "*await GetFalseAsync()*Result: False*");
    }

    [Test]
    public async Task Should_preserve_async_context()
    {
        await AssertDoesNotThrowAsync(async () =>
        {
            await Task.Yield();
            Assert(true);
        });
    }

    [Test]
    public async Task Should_handle_exceptions_in_async()
    {
        var action = async () => Assert(await ThrowingMethodAsync());

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