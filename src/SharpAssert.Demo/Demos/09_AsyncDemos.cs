using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class AsyncDemos
{
    static async Task<bool> GetBoolAsync()
    {
        await Task.Delay(10);
        return false;
    }

    static async Task<int> GetLeftValueAsync()
    {
        await Task.Delay(10);
        return 42;
    }

    static async Task<int> GetRightValueAsync()
    {
        await Task.Delay(10);
        return 100;
    }

    static async Task<string> GetStringAsync()
    {
        await Task.Delay(10);
        return "actual value";
    }

    /// <summary>
    /// Demonstrates basic async assertion with await.
    /// </summary>
    public static async Task BasicAwaitCondition()
    {
        Assert(await GetBoolAsync());
    }

    /// <summary>
    /// Demonstrates binary comparison with both sides awaited showing values.
    /// </summary>
    public static async Task AsyncBinaryComparison()
    {
        Assert(await GetLeftValueAsync() == await GetRightValueAsync());
    }

    /// <summary>
    /// Demonstrates mixed async and sync comparison.
    /// </summary>
    public static async Task MixedAsyncSync()
    {
        Assert(await GetLeftValueAsync() == 100);
    }

    /// <summary>
    /// Demonstrates async string comparison with diff.
    /// </summary>
    public static async Task AsyncStringDiff()
    {
        Assert(await GetStringAsync() == "expected value");
    }
}
