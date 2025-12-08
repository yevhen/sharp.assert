using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

public abstract class TestBase
{
    /// <summary>
    /// Tests that Sharp.Assert() throws with expected message pattern.
    /// Use this for testing via public API (goes through rewriter).
    /// </summary>
    protected static void AssertThrows(Action action, string expectedMessagePattern)
    {
        action.Should().Throw<SharpAssertionException>().WithMessage(expectedMessagePattern);
    }

    /// <summary>
    /// Tests that Sharp.Assert() does not throw.
    /// Use this for testing via public API (goes through rewriter).
    /// </summary>
    protected static void AssertDoesNotThrow(Action action)
    {
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that async Sharp.Assert() throws with expected message pattern.
    /// Use this for testing async assertions via public API (goes through rewriter).
    /// </summary>
    protected static async Task AssertThrowsAsync(Func<Task> action, string expectedMessagePattern)
    {
        await action.Should().ThrowAsync<SharpAssertionException>().WithMessage(expectedMessagePattern);
    }

    /// <summary>
    /// Tests that async Sharp.Assert() does not throw.
    /// Use this for testing async assertions via public API (goes through rewriter).
    /// </summary>
    protected static async Task AssertDoesNotThrowAsync(Func<Task> action)
    {
        await action.Should().NotThrowAsync();
    }
}