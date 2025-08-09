using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;

namespace SharpAssert;

[TestFixture]
public class ExceptionHandlingFixture : TestBase
{
    [Test]
    public void Should_handle_comparer_exceptions_in_EvaluateBinaryOperation()
    {
        var analyzerType = typeof(SharpInternal).Assembly.GetType("SharpAssert.ExpressionAnalyzer");
        var method = analyzerType!.GetMethod("EvaluateBinaryOperation", 
            BindingFlags.Static | BindingFlags.NonPublic);
        
        // Test with incompatible types that will cause Comparer<object>.Default.Compare to throw
        var result = method!.Invoke(null, [ExpressionType.LessThan, new NonComparableClass(), new DifferentNonComparableClass()]);
        
        result.Should().Be(false);
    }

    [Test]
    public void Should_handle_comparer_exceptions_in_real_expressions()
    {
        var list1 = new ArrayList { 1, 2, 3 };
        var list2 = new ArrayList { 4, 5, 6 };
        Expression<Func<bool>> expr = () => list1.Count < list2.Count;

        AssertExpressionThrows<SharpAssertionException>(expr, "list1.Count < list2.Count", "TestFile.cs", 500, "*3*3*");
    }
}

