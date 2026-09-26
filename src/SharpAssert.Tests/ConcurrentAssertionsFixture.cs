using FluentAssertions;
using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class ConcurrentAssertionsFixture
{
    [Test]
    public void Should_accept_independent_assertions_from_parallel_threads()
    {
        Parallel.For(0, 4000, new ParallelOptions { MaxDegreeOfParallelism = 16 }, index =>
        {
            var actual = index;
            var expected = index;
            Assert(actual == expected);
        });
    }

    [Test]
    public void Should_keep_failure_diagnostics_separate_across_parallel_threads()
    {
        Parallel.For(0, 500, new ParallelOptions { MaxDegreeOfParallelism = 16 }, index =>
        {
            var actual = index;
            var expected = -1;
            Action action = () => Assert(actual == expected);

            var error = action.Should().Throw<SharpAssertionException>().Which;
            error.Message.Should().Contain($"Left:  {actual}{Environment.NewLine}");
            error.Message.Should().Contain("Right: -1");
        });
    }
}
