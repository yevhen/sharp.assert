using FluentAssertions;
using System.Linq.Expressions;

namespace SharpAssert;

public abstract class TestBase
{
    protected static void AssertExpressionThrows<T>(Expression<Func<bool>> expression, string originalExpr, string file, int line, string expectedMessagePattern)
        where T : Exception
    {
        var action = () => SharpInternal.Assert(expression, originalExpr, file, line);
        action.Should().Throw<T>()
              .WithMessage(expectedMessagePattern);
    }

    protected static void AssertExpressionDoesNotThrow(Expression<Func<bool>> expression, string originalExpr, string file, int line, string? because = null)
    {
        var action = () => SharpInternal.Assert(expression, originalExpr, file, line);
        if (because != null)
            action.Should().NotThrow(because);
        else
            action.Should().NotThrow();
    }
}