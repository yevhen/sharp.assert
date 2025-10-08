using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class NullableTypeDemos
{
    /// <summary>
    /// Demonstrates nullable int with null value showing HasValue: false.
    /// </summary>
    public static void NullableIntWithNull()
    {
        int? value = null;
        Assert(value == 42);
    }

    /// <summary>
    /// Demonstrates nullable int with value showing HasValue: true, Value: 42.
    /// </summary>
    public static void NullableIntWithValue()
    {
        int? value = 42;
        Assert(value == 100);
    }

    /// <summary>
    /// Demonstrates nullable bool comparisons.
    /// </summary>
    public static void NullableBool()
    {
        bool? value = false;
        Assert(value == true);
    }

    /// <summary>
    /// Demonstrates nullable DateTime comparison with null.
    /// </summary>
    public static void NullableDateTime()
    {
        DateTime? value = null;
        var expected = DateTime.Now;
        Assert(value == expected);
    }

    /// <summary>
    /// Demonstrates nullable reference types (string?, object?).
    /// </summary>
    public static void NullableReferenceTypes()
    {
        string? value = null;
        string expected = "text";
        Assert(value == expected);
    }

    /// <summary>
    /// Demonstrates edge cases with nullable == null comparisons.
    /// </summary>
    public static void NullComparisonEdgeCases()
    {
        int? value = 42;
        Assert(value == null);
    }
}
