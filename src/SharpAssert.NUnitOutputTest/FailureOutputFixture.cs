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

        Assert(left == right && right == 3, "Primary diagnostics stay visible");
    }
}
