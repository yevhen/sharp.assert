namespace SharpAssert.PackageTest;

[TestFixture]
public class PackageTestFixture
{
    [Test]
    public void Should_support_basic_assertions_via_package()
    {
        var x = 5;
        var y = 10;
        
        Sharp.Assert(x < y);
    }

    [Test]
    public void Should_provide_detailed_error_messages_via_interceptors()
    {
        var items = new[] { 1, 2, 3 };
        var target = 999;
        
        var ex = Assert.Throws<SharpAssertionException>(() =>
            Sharp.Assert(items.Contains(target)));
        
        ex.Message.Should().Contain("items.Contains(target)");
        ex.Message.Should().NotBe("Assertion failed: items.Contains(target)");
    }
}