using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class DynamicDemos
{
    /// <summary>
    /// Demonstrates binary comparison with dynamic types showing actual values.
    /// </summary>
    public static void DynamicBinaryComparison()
    {
        dynamic value = 42;
        Assert(value == 100);
    }

    /// <summary>
    /// Demonstrates dynamic method call in assertion.
    /// </summary>
    public static void DynamicMethodCall()
    {
        dynamic obj = new DynamicObject();
        Assert(obj.GetValue() > 100);
    }

    /// <summary>
    /// Demonstrates dynamic operator semantics and evaluation.
    /// </summary>
    public static void DynamicOperatorSemantics()
    {
        dynamic left = 10;
        dynamic right = 20;
        Assert(left > right);
    }

    class DynamicObject
    {
        public int GetValue() => 42;
    }
}
