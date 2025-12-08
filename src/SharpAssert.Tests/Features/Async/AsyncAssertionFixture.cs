using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert.Features.Async;

[TestFixture]
public class AsyncAssertionFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public async Task Should_handle_await_in_condition()
        {
            await AssertPassesAsync(async () => Assert(await GetTrueAsync()));
        }

        [Test]
        public async Task Should_show_false_for_failed_async()
        {
            var expected = Value("await GetFalseAsync()", false, typeof(bool));
            
            await AssertFailsAsync(async () => Assert(await GetFalseAsync()), expected);
        }

        [Test]
        public async Task Should_preserve_async_context()
        {
            await AssertPassesAsync(async () =>
            {
                await Task.Yield();
                Assert(true);
            });
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_false_value()
        {
            var result = Value("await GetFalseAsync()", false, typeof(bool));
            AssertRendersExactly(result, "False");
        }
    }

    static async Task AssertPassesAsync(Func<Task> action)
    {
        await action.Should().NotThrowAsync();
    }

    static async Task<bool> GetTrueAsync()
    {
        await Task.Yield();
        return true;
    }

    static async Task<bool> GetFalseAsync()
    {
        await Task.Yield();
        return false;
    }
}
