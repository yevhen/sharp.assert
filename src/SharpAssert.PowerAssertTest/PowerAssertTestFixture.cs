using FluentAssertions;
using static SharpAssert.Sharp;

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
        // But we change it to "Assert failed, expression was:"
        ex.Message.Should().Contain("Assert failed, expression was:");
        ex.Message.Should().Contain("x > y");
    }
}