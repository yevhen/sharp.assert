using FluentAssertions;
using System.Linq.Expressions;

namespace SharpAssert;

[TestFixture]
public class CollectionComparisonFixture : TestBase
{
    [Test]
    public void Should_show_first_mismatch_index()
    {
        var left = new List<int> { 1, 2, 3 };
        var right = new List<int> { 1, 2, 4 };
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows<SharpAssertionException>(expr,
            "left == right", "CollectionComparisonFixture.cs", 11,
            "*First difference at index 2: expected 3, got 4*");
    }

    [Test]
    public void Should_show_missing_elements()
    {
        var left = new List<int> { 1, 2 };
        var right = new List<int> { 1, 2, 3 };
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows<SharpAssertionException>(expr,
            "left == right", "CollectionComparisonFixture.cs", 26,
            "*Missing elements: [3]*");
    }

    [Test]
    public void Should_show_extra_elements()
    {
        var left = new List<int> { 1, 2, 3 };
        var right = new List<int> { 1, 2 };
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows<SharpAssertionException>(expr,
            "left == right", "CollectionComparisonFixture.cs", 43,
            "*Extra elements: [3]*");
    }

    [Test]
    public void Should_handle_empty_collections()
    {
        var left = new List<int>();
        var right = new List<int> { 1 };
        Expression<Func<bool>> expr = () => left == right;

        AssertExpressionThrows<SharpAssertionException>(expr,
            "left == right", "CollectionComparisonFixture.cs", 60,
            "*Missing elements: [1]*");
    }
}