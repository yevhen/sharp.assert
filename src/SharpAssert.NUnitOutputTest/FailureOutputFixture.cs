using NUnit.Framework;
using static SharpAssert.Sharp;

namespace SharpAssert.NUnitOutputTest;

[TestFixture]
public class FailureOutputFixture
{
    [Test]
    public void Should_emit_compound_failure()
    {
        var left = 1;
        var right = 2;
        Func<bool> notReached = () => throw new InvalidOperationException("Not reached");

        Assert(left == right && right == 3 && left == 0 && notReached(), "Primary diagnostics stay visible");
    }
}
