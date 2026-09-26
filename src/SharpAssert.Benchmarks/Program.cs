using System.Diagnostics;
using System.Runtime.CompilerServices;
using static SharpAssert.Sharp;
using NUnitAssert = NUnit.Framework.Assert;

if (!System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.Contains(".NET 9"))
    Console.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} (roll-forward; compare on .NET 9 before release)");

Console.WriteLine("Release benchmark: median of 7 batches; one thread; ns/op and allocated bytes/op");
Console.WriteLine("Scenario,Iterations,ns/op,B/op");

var scenarios = new (string Name, int Iterations, Action<int> Run)[]
{
    ("Sharp rewritten pass", 20000, index => SharpPass(index, index)),
    ("Sharp boolean pass", 20000, index => SharpBooleanPass(index >= 0)),
    ("Sharp comparison with message pass", 20000, index => SharpMessagePass(index, index)),
    ("Sharp boolean property pass", 20000, index => SharpPropertyPass(ResultSink.Property)),
    ("Sharp expectation pass", 20000, index => SharpExpectationPass(index)),
    ("Sharp logical pass", 20000, index => SharpLogicalPass(index)),
    ("Sharp static two-argument pass", 20000, index => SharpStaticPass(index)),
    ("Sharp array SequenceEqual pass", 20000, index => SharpArraySequencePass(ResultSink.Values)),
    ("Sharp Contains pass", 20000, index => SharpContainsPass(ResultSink.Text)),
    ("Sharp StartsWith pass", 20000, index => SharpStartsWithPass(ResultSink.Text)),
    ("Sharp direct pass (basic diagnostics)", 20000, index => SharpDirectPass(index, index)),
    ("NUnit constraint pass", 20000, index => NUnitConstraintPass(index, index)),
    ("NUnit boolean pass", 20000, index => NUnitBooleanPass(index, index)),
    ("Sharp rewritten fail", 1000, index => SharpFail(index, index + 1)),
    ("Sharp boolean fail", 1000, index => SharpBooleanFail(index < 0)),
    ("Sharp comparison with message fail", 1000, index => SharpMessageFail(index, index + 1)),
    ("Sharp logical fail", 1000, index => SharpLogicalFail(index)),
    ("Sharp array SequenceEqual fail", 1000, index => SharpArraySequenceFail(ResultSink.Values, ResultSink.OtherValues)),
    ("Sharp direct fail (basic diagnostics)", 1000, index => SharpDirectFail(index, index + 1)),
    ("NUnit constraint fail", 1000, index => NUnitConstraintFail(index, index + 1)),
    ("NUnit boolean fail", 1000, index => NUnitBooleanFail(index, index + 1))
};

foreach (var (name, iterations, run) in scenarios)
{
    var warmup = iterations == 1000 ? 200 : 200000;
    for (var index = 0; index < warmup; index++)
        run(index);

    var times = new double[7];
    var allocations = new double[7];
    for (var sample = 0; sample < times.Length; sample++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++)
            run(index);
        var elapsed = Stopwatch.GetElapsedTime(start);
        times[sample] = elapsed.TotalNanoseconds / iterations;
        allocations[sample] = (double)(GC.GetAllocatedBytesForCurrentThread() - before) / iterations;
    }

    Console.WriteLine($"{name},{iterations},{times.Order().ElementAt(3):F0},{allocations.Order().ElementAt(3):F0}");
}

Console.WriteLine("Parallel pass (up to 4 workers),Operations,ops/s");
foreach (var (name, _, run) in scenarios.Take(7))
{
    var throughputs = new double[7];
    for (var sample = 0; sample < throughputs.Length; sample++)
    {
        var start = Stopwatch.GetTimestamp();
        Parallel.For(0, 200000, new ParallelOptions { MaxDegreeOfParallelism = 4 }, run);
        throughputs[sample] = 200000 / Stopwatch.GetElapsedTime(start).TotalSeconds;
    }

    Console.WriteLine($"{name},200000,{throughputs.Order().ElementAt(3):F0}");
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpPass(int actual, int expected)
{
    Assert(actual == expected);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpDirectPass(int actual, int expected) => SharpAssert.Sharp.Assert(actual == expected);

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpBooleanPass(bool actual)
{
    Assert(actual);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpMessagePass(int actual, int expected)
{
    Assert(actual == expected, "Values differ");
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpPropertyPass(BooleanProperty value)
{
    Assert(value.IsValid);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpExpectationPass(int index)
{
    Assert(SharpAssert.Expectation.From(() => index >= 0, () => ["Negative"]));
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpLogicalPass(int index)
{
    Assert(index >= 0 && index < 200001);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpStaticPass(int index)
{
    Assert(BenchmarkPredicates.IsEvenSum(index, index));
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpArraySequencePass(int[] values)
{
    Assert(values.SequenceEqual(values));
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpContainsPass(string value)
{
    var needle = "arp";
    Assert(value.Contains(needle));
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpStartsWithPass(string value)
{
    Assert(value.StartsWith("Sha"));
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void NUnitConstraintPass(int actual, int expected) => NUnitAssert.That(actual, NUnit.Framework.Is.EqualTo(expected));

[MethodImpl(MethodImplOptions.NoInlining)]
static void NUnitBooleanPass(int actual, int expected) => NUnitAssert.That(actual == expected);

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpFail(int actual, int expected)
{
    try
    {
        Assert(actual == expected);
        throw new InvalidOperationException("Expected SharpAssert failure");
    }
    catch (SharpAssert.SharpAssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpBooleanFail(bool actual)
{
    try
    {
        Assert(actual);
        throw new InvalidOperationException("Expected SharpAssert failure");
    }
    catch (SharpAssert.SharpAssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpMessageFail(int actual, int expected)
{
    try
    {
        Assert(actual == expected, "Values differ");
        throw new InvalidOperationException("Expected SharpAssert failure");
    }
    catch (SharpAssert.SharpAssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpLogicalFail(int index)
{
    try
    {
        Assert(index >= 0 && index < 0);
        throw new InvalidOperationException("Expected SharpAssert failure");
    }
    catch (SharpAssert.SharpAssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpArraySequenceFail(int[] actual, int[] expected)
{
    try
    {
        Assert(actual.SequenceEqual(expected));
        throw new InvalidOperationException("Expected SharpAssert failure");
    }
    catch (SharpAssert.SharpAssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void SharpDirectFail(int actual, int expected)
{
    try
    {
        SharpAssert.Sharp.Assert(actual == expected);
        throw new InvalidOperationException("Expected SharpAssert failure");
    }
    catch (SharpAssert.SharpAssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void NUnitConstraintFail(int actual, int expected)
{
    try
    {
        NUnitAssert.That(actual, NUnit.Framework.Is.EqualTo(expected));
        throw new InvalidOperationException("Expected NUnit failure");
    }
    catch (NUnit.Framework.AssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void NUnitBooleanFail(int actual, int expected)
{
    try
    {
        NUnitAssert.That(actual == expected);
        throw new InvalidOperationException("Expected NUnit failure");
    }
    catch (NUnit.Framework.AssertionException error)
    {
        Volatile.Write(ref ResultSink.Last, error);
    }
}

static class BenchmarkPredicates
{
    public static bool IsEvenSum(int left, int right) => (left + right) % 2 == 0;
}

sealed class BooleanProperty
{
    public bool IsValid => true;
}

static class ResultSink
{
    public static object? Last;
    public static readonly BooleanProperty Property = new();
    public static readonly string Text = "SharpAssert";
    public static readonly int[] Values = [1, 2, 3];
    public static readonly int[] OtherValues = [1, 2, 4];
}
