using NUnit.Framework;
using static SharpAssert.Sharp;

namespace SharpAssert.NUnitOutputTest;

[TestFixture]
public class FailureOutputFixture
{
    [Test]
    public void Should_emit_compound_failure()
    {
        var status = Status.Running;
        var total = 1;
        Func<bool> notReached = () => throw new InvalidOperationException("Not reached");

        Assert(status == Status.Completed && total == 3 && total == 0 && notReached(), "Primary diagnostics stay visible");
    }

    enum Status { Running = 1, Completed = 2 }
}
