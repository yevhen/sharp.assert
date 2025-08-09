using System.Linq.Expressions;

namespace SharpAssert;

[TestFixture]
public class LogicalOperatorAnalysisFixture : TestBase
{
    [Test]
    public void Should_not_fail_when_left_operand_of_or_is_true()
    {
        var leftTrue = true;
        var rightFalse = false;
        
        Expression<Func<bool>> expr = () => leftTrue || rightFalse;

        AssertExpressionDoesNotThrow(expr, "leftTrue || rightFalse", "TestFile.cs", 600, "because leftTrue || rightFalse == true");
    }

    [Test]
    public void Should_fail_when_both_operands_of_or_are_false()
    {
        var leftFalse = false;
        var rightFalse = false;
        
        Expression<Func<bool>> expr = () => leftFalse || rightFalse;

        AssertExpressionThrows<SharpAssertionException>(expr, "leftFalse || rightFalse", "TestFile.cs", 601, "*Both operands were false*");
    }

    [Test]
    public void Should_not_fail_when_right_operand_of_or_is_true()
    {
        var leftFalse = false;
        var rightTrue = true;
        
        Expression<Func<bool>> expr = () => leftFalse || rightTrue;

        AssertExpressionDoesNotThrow(expr, "leftFalse || rightTrue", "TestFile.cs", 602, "because leftFalse || rightTrue == true");
    }

    [Test]
    public void Should_fail_when_left_operand_of_and_is_false()
    {
        var leftFalse = false;
        var rightTrue = true;
        
        Expression<Func<bool>> expr = () => leftFalse && rightTrue;

        AssertExpressionThrows<SharpAssertionException>(expr, "leftFalse && rightTrue", "TestFile.cs", 604, "*Left operand was false*");
    }

    [Test]
    public void Should_fail_when_right_operand_of_and_is_false()
    {
        var leftTrue = true;
        var rightFalse = false;
        
        Expression<Func<bool>> expr = () => leftTrue && rightFalse;

        AssertExpressionThrows<SharpAssertionException>(expr, "leftTrue && rightFalse", "TestFile.cs", 605, "*Right operand was false*");
    }
}