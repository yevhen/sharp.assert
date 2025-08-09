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
        // Arrange
        int x = 42;
        int y = 24;
        Expression<Func<bool>> expr = () => x == y;

        // Act & Assert
        var action = () => SharpInternal.Assert(expr, "x == y", "TestFile.cs", 123);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*42*24*"); // Should show both operand values
    }

    [Test]
    public void Should_handle_all_comparison_operators()
    {
        // Test ==
        int x = 5;
        int y = 10;
        Expression<Func<bool>> exprEq = () => x == y;
        var actionEq = () => SharpInternal.Assert(exprEq, "x == y", "TestFile.cs", 1);
        actionEq.Should().Throw<SharpAssertionException>()
                .WithMessage("*5*10*");

        // Test !=  
        int a = 5;
        int b = 5;
        Expression<Func<bool>> exprNe = () => a != b;
        var actionNe = () => SharpInternal.Assert(exprNe, "a != b", "TestFile.cs", 2);
        actionNe.Should().Throw<SharpAssertionException>()
                .WithMessage("*5*5*");

        // Test <
        int c = 10;
        int d = 5;
        Expression<Func<bool>> exprLt = () => c < d;
        var actionLt = () => SharpInternal.Assert(exprLt, "c < d", "TestFile.cs", 3);
        actionLt.Should().Throw<SharpAssertionException>()
                .WithMessage("*10*5*");

        // Test <=
        int e = 10;
        int f = 5;
        Expression<Func<bool>> exprLe = () => e <= f;
        var actionLe = () => SharpInternal.Assert(exprLe, "e <= f", "TestFile.cs", 4);
        actionLe.Should().Throw<SharpAssertionException>()
                .WithMessage("*10*5*");

        // Test >
        int g = 5;
        int h = 10;
        Expression<Func<bool>> exprGt = () => g > h;
        var actionGt = () => SharpInternal.Assert(exprGt, "g > h", "TestFile.cs", 5);
        actionGt.Should().Throw<SharpAssertionException>()
                .WithMessage("*5*10*");

        // Test >=
        int i = 5;
        int j = 10;
        Expression<Func<bool>> exprGe = () => i >= j;
        var actionGe = () => SharpInternal.Assert(exprGe, "i >= j", "TestFile.cs", 6);
        actionGe.Should().Throw<SharpAssertionException>()
                .WithMessage("*5*10*");
    }

    [Test]
    public void Should_handle_null_operands()
    {
        // Arrange
        string? nullString = null;
        string value = "test";
        Expression<Func<bool>> expr = () => nullString == value;

        // Act & Assert
        var action = () => SharpInternal.Assert(expr, "nullString == value", "TestFile.cs", 100);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*null*test*"); // Should show "null" for null values
    }

    private int _callCount = 0;
    
    private int GetValueAndIncrement()
    {
        _callCount++;
        return _callCount * 10;
    }

    [Test]
    public void Should_evaluate_complex_expressions_once()
    {
        // Arrange - reset call count
        _callCount = 0;

        // Create expression that calls the method twice in different sides
        Expression<Func<bool>> expr = () => GetValueAndIncrement() == GetValueAndIncrement();

        // Act & Assert - this should fail because 10 != 20, but each side should only be evaluated once
        var action = () => SharpInternal.Assert(expr, "GetValueAndIncrement() == GetValueAndIncrement()", "TestFile.cs", 200);
        action.Should().Throw<SharpAssertionException>();
        
        // The key test: each method should have been called exactly once during evaluation
        _callCount.Should().Be(2, "each operand should be evaluated exactly once");
    }
}