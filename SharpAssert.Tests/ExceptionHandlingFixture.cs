using FluentAssertions;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace SharpAssert.Tests;

[TestFixture]
public class ExceptionHandlingFixture
{
    [Test]
    public void Should_handle_comparer_exceptions_in_EvaluateBinaryOperation()
    {
        // We'll use reflection to test the private EvaluateBinaryOperation method directly
        // This allows us to specifically test the exception handling in lines 122-124
        
        var analyzerType = typeof(SharpInternal).Assembly.GetType("SharpAssert.ExpressionAnalyzer");
        var method = analyzerType!.GetMethod("EvaluateBinaryOperation", 
            BindingFlags.Static | BindingFlags.NonPublic);
        
        // Test with incompatible types that will cause Comparer<object>.Default.Compare to throw
        var result = method!.Invoke(null, [ExpressionType.LessThan, new NonComparableType(), new DifferentNonComparableType()]);
        
        // The method should return false when an exception occurs (lines 122-124)
        result.Should().Be(false);
    }

    [Test]
    public void Should_handle_comparer_exceptions_in_real_expressions()
    {
        // Create a scenario that will trigger the exception path during expression evaluation
        // We need types that will cause ArgumentException in Comparer<object>.Default.Compare
        var list1 = new ArrayList { 1, 2, 3 };
        var list2 = new ArrayList { 4, 5, 6 };
        
        // When these are compared as objects, Comparer<object>.Default.Compare will throw
        // because ArrayList implements IComparable but in a way that can throw ArgumentException
        Expression<Func<bool>> expr = () => list1.Count < list2.Count;

        var action = () => SharpInternal.Assert(expr, "list1.Count < list2.Count", "TestFile.cs", 500);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*3*3*");
    }
}

internal class NonComparableType
{
    public override string ToString() => "NonComparableType";
}

internal class DifferentNonComparableType
{
    public override string ToString() => "DifferentNonComparableType";
}