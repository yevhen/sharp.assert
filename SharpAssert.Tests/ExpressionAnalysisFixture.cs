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
        var x = 42;
        var y = 24;
        Expression<Func<bool>> expr = () => x == y;

        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 123);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*42*24*");
    }

    [Test]
    public void Should_handle_equality_operator()
    {
        var x = 5;
        var y = 10;
        Expression<Func<bool>> expr = () => x == y;

        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 1);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_inequality_operator()
    {
        var a = 5;
        var b = 5;
        Expression<Func<bool>> expr = () => a != b;

        var action = () => SharpInternal.Assert(expr, "a != b", "TestFile.cs", 2);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*5*");
    }

    [Test]
    public void Should_handle_less_than_operator()
    {
        var c = 10;
        var d = 5;
        Expression<Func<bool>> expr = () => c < d;

        var action = () => SharpInternal.Assert(expr, "c < d", "TestFile.cs", 3);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*10*5*");
    }

    [Test]
    public void Should_handle_less_than_or_equal_operator()
    {
        var e = 10;
        var f = 5;
        Expression<Func<bool>> expr = () => e <= f;

        var action = () => SharpInternal.Assert(expr, "e <= f", "TestFile.cs", 4);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*10*5*");
    }

    [Test]
    public void Should_handle_greater_than_operator()
    {
        var g = 5;
        var h = 10;
        Expression<Func<bool>> expr = () => g > h;

        var action = () => SharpInternal.Assert(expr, "g > h", "TestFile.cs", 5);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_greater_than_or_equal_operator()
    {
        var i = 5;
        var j = 10;
        Expression<Func<bool>> expr = () => i >= j;

        var action = () => SharpInternal.Assert(expr, "i >= j", "TestFile.cs", 6);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_null_operands()
    {
        string? nullString = null;
        var value = "test";
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