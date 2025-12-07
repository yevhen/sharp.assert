using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class CollectionComparisonFixture : TestBase
{
    [Test]
    public void Should_show_first_mismatch_index()
    {
        var left = new List<int> { 1, 2, 3 };
        var right = new List<int> { 1, 2, 4 };

        AssertThrows(() => Assert(left == right),
            "*First difference at index 2: expected 3, got 4*");
    }

    [Test]
    public void Should_show_missing_elements()
    {
        var left = new List<int> { 1, 2 };
        var right = new List<int> { 1, 2, 3 };

        AssertThrows(() => Assert(left == right),
            "*Missing elements: [3]*");
    }

    [Test]
    public void Should_show_extra_elements()
    {
        var left = new List<int> { 1, 2, 3 };
        var right = new List<int> { 1, 2 };

        AssertThrows(() => Assert(left == right),
            "*Extra elements: [3]*");
    }

    [Test]
    public void Should_handle_empty_collections()
    {
        var left = new List<int>();
        var right = new List<int> { 1 };

        AssertThrows(() => Assert(left == right),
            "*Missing elements: [1]*");
    }

    [Test]
    public void Should_pass_when_collections_are_equal()
    {
        var left = new List<int> { 1, 2, 3 };
        var right = new List<int> { 1, 2, 3 };
        AssertDoesNotThrow(() => Assert(left.SequenceEqual(right)));
    }

    [Test]
    public void Should_pass_when_empty_collections_are_equal()
    {
        var left = new List<int>();
        var right = new List<int>();
        AssertDoesNotThrow(() => Assert(left.SequenceEqual(right)));
    }

    [Test]
    public void Should_pass_with_different_collection_types()
    {
        var list = new List<int> { 1, 2, 3 };
        var array = new[] { 1, 2, 3 };
        AssertDoesNotThrow(() => Assert(list.SequenceEqual(array)));
    }
}