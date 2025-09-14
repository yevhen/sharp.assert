using System.Collections;
using System.Linq.Expressions;

namespace SharpAssert;

[TestFixture]
public class ExceptionHandlingFixture : TestBase
{
    [Test]
    public void Should_handle_comparer_exceptions_in_real_expressions()
    {
        var list1 = new ArrayList { 1, 2, 3 };
        var list2 = new ArrayList { 4, 5, 6 };
        Expression<Func<bool>> expr = () => list1.Count < list2.Count;

        AssertExpressionThrows(expr, "list1.Count < list2.Count", "TestFile.cs", 500, "*3*3*");
    }

    [Test]
    public void Should_pass_when_arraylist_count_comparison_succeeds()
    {
        var list1 = new ArrayList { 1, 2, 3 };
        var list2 = new ArrayList { 4, 5, 6, 7 };

        AssertExpressionPasses(() => list1.Count < list2.Count);
    }

    [Test]
    public void Should_pass_when_arraylist_equality_succeeds()
    {
        var list1 = new ArrayList { 1, 2, 3 };
        var list2 = new ArrayList { 1, 2, 3 };

        AssertExpressionPasses(() => list1.Count == list2.Count);
    }

    [Test]
    public void Should_pass_when_arraylist_greater_than_comparison_succeeds()
    {
        var list1 = new ArrayList { 1, 2, 3, 4, 5 };
        var list2 = new ArrayList { 1, 2 };

        AssertExpressionPasses(() => list1.Count > list2.Count);
    }

    [Test]
    public void Should_pass_when_arraylist_less_than_or_equal_comparison_succeeds()
    {
        var list1 = new ArrayList { 1, 2 };
        var list2 = new ArrayList { 1, 2 };

        AssertExpressionPasses(() => list1.Count <= list2.Count);
    }
}

