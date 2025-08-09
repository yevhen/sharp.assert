using FluentAssertions;
using SharpAssert;
using System.Linq.Expressions;

namespace SharpAssert.Tests;

[TestFixture]
public class ExpressionAnalysisFixture
{
    [Test]
    public void Should_show_left_and_right_values_for_equality()
    {
        int x = 42;
        int y = 24;
        Expression<Func<bool>> expr = () => x == y;

        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 123);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*42*24*");
    }

    [Test]
    public void Should_handle_equality_operator()
    {
        int x = 5;
        int y = 10;
        Expression<Func<bool>> expr = () => x == y;

        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 1);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_inequality_operator()
    {
        int a = 5;
        int b = 5;
        Expression<Func<bool>> expr = () => a != b;

        var action = () => SharpInternal.Assert(expr, "a != b", "TestFile.cs", 2);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*5*");
    }

    [Test]
    public void Should_handle_less_than_operator()
    {
        int c = 10;
        int d = 5;
        Expression<Func<bool>> expr = () => c < d;

        var action = () => SharpInternal.Assert(expr, "c < d", "TestFile.cs", 3);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*10*5*");
    }

    [Test]
    public void Should_handle_less_than_or_equal_operator()
    {
        int e = 10;
        int f = 5;
        Expression<Func<bool>> expr = () => e <= f;

        var action = () => SharpInternal.Assert(expr, "e <= f", "TestFile.cs", 4);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*10*5*");
    }

    [Test]
    public void Should_handle_greater_than_operator()
    {
        int g = 5;
        int h = 10;
        Expression<Func<bool>> expr = () => g > h;

        var action = () => SharpInternal.Assert(expr, "g > h", "TestFile.cs", 5);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_greater_than_or_equal_operator()
    {
        int i = 5;
        int j = 10;
        Expression<Func<bool>> expr = () => i >= j;

        var action = () => SharpInternal.Assert(expr, "i >= j", "TestFile.cs", 6);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_null_operands()
    {
        string? nullString = null;
        string value = "test";
        Expression<Func<bool>> expr = () => nullString == value;

        var action = () => SharpInternal.Assert(expr, "nullString == value", "TestFile.cs", 100);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*null*test*");
    }

    int callCount = 0;
    
    int GetValueAndIncrement()
    {
        callCount++;
        return callCount * 10;
    }

    [Test]
    public void Should_evaluate_complex_expressions_once()
    {
        callCount = 0;

        Expression<Func<bool>> expr = () => GetValueAndIncrement() == GetValueAndIncrement();

        var action = () => SharpInternal.Assert(expr, "GetValueAndIncrement() == GetValueAndIncrement()", "TestFile.cs", 200);
        action.Should().Throw<SharpAssertionException>();
        
        callCount.Should().Be(2, "each operand should be evaluated exactly once");
    }
}