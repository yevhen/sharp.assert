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

        AssertExpressionThrows<SharpAssertionException>(expr, "list1.Count < list2.Count", "TestFile.cs", 500, "*3*3*");
    }
}

