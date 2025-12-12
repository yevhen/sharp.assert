using FluentAssertions;
using SharpAssert.Features.Shared;

namespace SharpAssert.Features;

[TestFixture]
public class ValueFormatterFixture
{
    [Test]
    public void Should_render_unavailable_sentinel()
    {
        var unavailable = new EvaluationUnavailable("reason");

        ValueFormatter.Format(unavailable).Should().Be("<unavailable: reason>");
    }
}
