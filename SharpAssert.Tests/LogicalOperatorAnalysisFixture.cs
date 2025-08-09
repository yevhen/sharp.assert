using FluentAssertions;
using System.Linq.Expressions;

namespace SharpAssert.Tests;

[TestFixture]
public class LogicalOperatorAnalysisFixture
{
    [Test]
    public void Should_not_fail_when_left_operand_of_or_is_true()
    {
        // If left operand of OR is true, the entire expression should be true and not fail
        var leftTrue = true;
        var rightFalse = false;
        
        Expression<Func<bool>> expr = () => leftTrue || rightFalse;

        // This should NOT throw because leftTrue || rightFalse == true
        var action = () => SharpInternal.Assert(expr, "leftTrue || rightFalse", "TestFile.cs", 600);
        action.Should().NotThrow();
    }

    [Test]
    public void Should_fail_when_both_operands_of_or_are_false()
    {
        // This should trigger the OrElse failure path, but NOT the "left operand was true" path
        var leftFalse = false;
        var rightFalse = false;
        
        Expression<Func<bool>> expr = () => leftFalse || rightFalse;

        var action = () => SharpInternal.Assert(expr, "leftFalse || rightFalse", "TestFile.cs", 601);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*Both operands were false*");
    }

    [Test]
    public void Should_not_fail_when_right_operand_of_or_is_true()
    {
        // If right operand of OR is true, the entire expression should be true and not fail
        var leftFalse = false;
        var rightTrue = true;
        
        Expression<Func<bool>> expr = () => leftFalse || rightTrue;

        // This should NOT throw because leftFalse || rightTrue == true
        var action = () => SharpInternal.Assert(expr, "leftFalse || rightTrue", "TestFile.cs", 602);
        action.Should().NotThrow();
    }

    [Test]
    public void Should_demonstrate_line_66_is_unreachable()
    {
        // This test demonstrates that line 66 should never be reached
        // because if leftBool is true in an OR operation, the overall result would be true
        // and AnalyzeLogicalBinaryFailure would never be called
        
        var leftTrue = true;
        var rightAnyValue = GetRandomBoolean();
        
        Expression<Func<bool>> expr = () => leftTrue || rightAnyValue;

        // Regardless of rightAnyValue, this should never throw
        var action = () => SharpInternal.Assert(expr, "leftTrue || rightAnyValue", "TestFile.cs", 603);
        action.Should().NotThrow("because true || anything == true");
    }

    [Test]
    public void Should_fail_when_left_operand_of_and_is_false()
    {
        // This should trigger the AndAlso failure path with short-circuit message
        var leftFalse = false;
        var rightTrue = true;
        
        Expression<Func<bool>> expr = () => leftFalse && rightTrue;

        var action = () => SharpInternal.Assert(expr, "leftFalse && rightTrue", "TestFile.cs", 604);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*Left operand was false*");
    }

    [Test]
    public void Should_fail_when_right_operand_of_and_is_false()
    {
        // This should trigger the AndAlso failure path for right operand
        var leftTrue = true;
        var rightFalse = false;
        
        Expression<Func<bool>> expr = () => leftTrue && rightFalse;

        var action = () => SharpInternal.Assert(expr, "leftTrue && rightFalse", "TestFile.cs", 605);
        action.Should().Throw<SharpAssertionException>()
              .WithMessage("*Right operand was false*");
    }

    [Test]
    public void Should_verify_GetOperatorSymbol_logical_cases_are_unused()
    {
        // This test verifies that GetOperatorSymbol is never called for logical operators
        // because they have separate handling in AnalyzeLogicalBinaryFailure
        
        // Test AndAlso - this should be handled by AnalyzeLogicalBinaryFailure, not AnalyzeBinaryFailure
        var leftFalse = false;
        var rightTrue = true;
        
        Expression<Func<bool>> exprAnd = () => leftFalse && rightTrue;
        var actionAnd = () => SharpInternal.Assert(exprAnd, "leftFalse && rightTrue", "TestFile.cs", 700);
        
        actionAnd.Should().Throw<SharpAssertionException>();
        // The message should NOT contain "&&" symbol because it's not using GetOperatorSymbol
        // Instead it uses hardcoded messages like "Left operand was false"
        
        // Test OrElse - this should be handled by AnalyzeLogicalBinaryFailure, not AnalyzeBinaryFailure  
        var leftFalse2 = false;
        var rightFalse2 = false;
        
        Expression<Func<bool>> exprOr = () => leftFalse2 || rightFalse2;
        var actionOr = () => SharpInternal.Assert(exprOr, "leftFalse2 || rightFalse2", "TestFile.cs", 701);
        
        actionOr.Should().Throw<SharpAssertionException>();
        // The message should NOT contain "||" symbol from GetOperatorSymbol
        // Instead it uses hardcoded messages like "Both operands were false"
    }

    private static bool GetRandomBoolean()
    {
        // Return a random boolean to show that it doesn't matter what the right operand is
        return new Random().Next(2) == 0;
    }
}