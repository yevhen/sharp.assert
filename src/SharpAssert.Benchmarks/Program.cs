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
    ("Sharp direct pass (basic diagnostics)", 20000, index => SharpDirectPass(index, index)),
    ("NUnit constraint pass", 20000, index => NUnitConstraintPass(index, index)),
    ("NUnit boolean pass", 20000, index => NUnitBooleanPass(index, index)),
    ("Sharp rewritten fail", 1000, index => SharpFail(index, index + 1)),
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
foreach (var (name, _, run) in scenarios.Take(3))
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

static class ResultSink
{
    public static object? Last;
}
