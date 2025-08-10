using FluentAssertions;
using static Sharp;
using static NUnit.Framework.Assert;

namespace SharpAssert.PowerAssertTest;

[TestFixture]
public class PowerAssertTestFixture
{
    [Test]
    public void Should_use_powerassert_when_enabled()
    {
        var x = 5;
        var y = 10;

        var ex = Throws<SharpAssertionException>(() =>
            Assert(x > y))!;

        // PowerAssert's distinctive error format includes "IsTrue failed, expression was:"
        ex.Message.Should().Contain("IsTrue failed, expression was:");
        ex.Message.Should().Contain("x > y");
    }
}