using System.Diagnostics;
using static SharpAssert.Sharp;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class AssertDiscriminationFixture : TestBase
{
    [Test]
    public void Should_not_rewrite_debug_assert()
    {
        var obj = new object();

        var action = () => Debug.Assert(obj != null, nameof(obj) + " != null");

        action.Should().NotThrow<SharpAssertionException>("Debug.Assert should not be rewritten by SharpAssert");
    }

    [Test]
    public void Should_not_rewrite_nunit_framework_assert()
    {
        var action = () => NUnit.Framework.Assert.That(1, Is.EqualTo(1));

        action.Should().NotThrow("NUnit.Framework.Assert.That should work normally when not rewritten");
    }

    [Test]
    public void Should_handle_special_characters_in_custom_message()
    {
        var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(false, "Error: \"quoted\" text"));
        exception.Result!.Context.Message.Should().Be("Error: \"quoted\" text");
    }

    [Test]
    public void Should_handle_escaped_characters_in_custom_message()
    {
        var exception = NUnit.Framework.Assert.Throws<SharpAssertionException>(() => Assert(1 == 2, "Path: C:\\Users\\Test"));
        exception.Result!.Context.Message.Should().Be("Path: C:\\Users\\Test");
    }

    [Test]
    public void Should_not_rewrite_sharp_assert_alongside_debug_assert()
    {
        var x = 1;
        var obj = new object();

        Debug.Assert(obj != null, "This should not be rewritten");

        AssertPasses(() => Assert(x == 1));
    }
}