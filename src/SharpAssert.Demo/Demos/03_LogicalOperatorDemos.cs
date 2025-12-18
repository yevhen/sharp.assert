using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class LogicalOperatorDemos
{
    /// <summary>
    /// Demonstrates AND operator failure showing which operand failed.
    /// </summary>
    public static void AndOperatorFailure()
    {
        var left = true;
        var right = false;
        Assert(left && right);
    }

    /// <summary>
    /// Demonstrates AND showing ALL failures when both operands fail (no short-circuit).
    /// Similar to NUnit Assert.Multiple or FluentAssertions AssertionScope but with native syntax.
    /// </summary>
    public static void AndBothFailed()
    {
        var x = 3;
        var y = 7;
        Assert(x == 5 && y == 10);
    }

    /// <summary>
    /// Demonstrates OR operator failure showing both operands were evaluated.
    /// </summary>
    public static void OrOperatorFailure()
    {
        var left = false;
        var right = false;
        Assert(left || right);
    }

    /// <summary>
    /// Demonstrates NOT operator showing the actual value that was negated.
    /// </summary>
    public static void NotOperator()
    {
        var value = true;
        Assert(!value);
    }

    /// <summary>
    /// Demonstrates complex nested logical expressions with multiple operators.
    /// </summary>
    public static void NestedLogical()
    {
        var a = true;
        var b = false;
        var c = true;
        var d = false;
        Assert((a && b) || (c && d));
    }
}
