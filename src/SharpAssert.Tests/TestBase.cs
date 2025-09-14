using FluentAssertions;
using System.Linq.Expressions;

namespace SharpAssert;

public abstract class TestBase
{
    protected static void AssertExpressionThrows(Expression<Func<bool>> expression, string originalExpr, string file,
        int line, string expectedMessagePattern)
    {
        var action = () => SharpInternal.Assert(expression, originalExpr, file, line, null);
        action.Should().Throw<SharpAssertionException>().WithMessage(expectedMessagePattern);
    }

    protected static void AssertExpressionPasses(Expression<Func<bool>> expression)
    {
        var action = () => SharpInternal.Assert(expression, expression.ToString(), "TestFile.cs", 1);
        action.Should().NotThrow();
    }
}