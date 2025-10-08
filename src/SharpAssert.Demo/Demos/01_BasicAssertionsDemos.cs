using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class BasicAssertionsDemos
{
    /// <summary>
    /// Demonstrates a simple assertion failure showing the expression text.
    /// </summary>
    public static void SimpleFailure()
    {
        Assert(false);
    }

    /// <summary>
    /// Demonstrates assertion failure with expression showing operands and result.
    /// </summary>
    public static void ExpressionText()
    {
        Assert(1 == 2);
    }

    /// <summary>
    /// Demonstrates assertion failure with a custom message.
    /// </summary>
    public static void CustomMessage()
    {
        Assert(false, "This is a custom failure message");
    }

    /// <summary>
    /// Demonstrates complex expression with multiple variables and operators.
    /// </summary>
    public static void ComplexExpression()
    {
        var x = 10;
        var y = 5;
        var z = 3;
        Assert(x + y * z > 100);
    }
}
