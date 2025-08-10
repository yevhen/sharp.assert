using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class CollectionComparisonFixture : TestBase
{
    [Test]
    [Ignore("Feature not yet implemented - Increment 6")]
    public void Should_show_first_mismatch_index()
    {
        // Assert([1,2,3] == [1,2,4]) should show index 2 as first mismatch
        // Expected: "First difference at index 2: expected 3, got 4"
        Assert.Fail("Collection first mismatch detection not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 6")]
    public void Should_show_missing_elements()
    {
        // Assert([1,2] == [1,2,3]) should show missing element 3
        // Expected: "Missing elements: [3]"
        Assert.Fail("Collection missing elements detection not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 6")]
    public void Should_show_extra_elements()
    {
        // Assert([1,2,3] == [1,2]) should show extra element 3
        // Expected: "Extra elements: [3]"
        Assert.Fail("Collection extra elements detection not yet implemented");
    }

    [Test]
    [Ignore("Feature not yet implemented - Increment 6")]
    public void Should_handle_empty_collections()
    {
        // Assert([] == [1]) should be handled correctly
        // Expected: Clear indication of empty vs non-empty collection
        Assert.Fail("Empty collection handling not yet implemented");
    }
}