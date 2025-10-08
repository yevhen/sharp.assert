using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class BinaryComparisonDemos
{
    /// <summary>
    /// Demonstrates equality and inequality operators with different types.
    /// </summary>
    public static void EqualityOperators()
    {
        var actual = 42;
        var expected = 100;
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates relational operators (less than, greater than, etc.) with numbers.
    /// </summary>
    public static void RelationalOperators()
    {
        var value = 5;
        var threshold = 10;
        Assert(value > threshold);
    }

    /// <summary>
    /// Demonstrates null comparisons showing left and right operand values.
    /// </summary>
    public static void NullComparisons()
    {
        string? nullValue = null;
        var nonNullValue = "text";
        Assert(nullValue == nonNullValue);
    }

    static int callCount = 0;
    static int GetValue()
    {
        callCount++;
        return 42;
    }

    /// <summary>
    /// Demonstrates that expressions are only evaluated once (single evaluation guarantee).
    /// </summary>
    public static void SingleEvaluationDemo()
    {
        callCount = 0;
        Assert(GetValue() == 100);
    }

    /// <summary>
    /// Demonstrates comparing different types showing type information.
    /// </summary>
    public static void TypeMismatch()
    {
        object intValue = 42;
        object stringValue = "42";
        Assert(intValue.Equals(stringValue));
    }
}
