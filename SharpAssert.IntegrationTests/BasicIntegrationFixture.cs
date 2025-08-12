using static Sharp;
using FluentAssertions;
using NUnit.Framework;

namespace SharpAssert.IntegrationTests;

[TestFixture]
public class BasicIntegrationFixture
{
    [Test]
    public void Assert_rewriting()
    {
        var x = 5;
        var y = 10;
        
        var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => 
            Assert(x == y));
        
        exception.Should().NotBeNull();
        
        exception.Message.Should().Contain("x == y");
        exception.Message.Should().Contain("x: 5");
        exception.Message.Should().Contain("y: 10");
    }
}