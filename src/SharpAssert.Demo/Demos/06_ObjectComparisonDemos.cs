using static SharpAssert.Sharp;

namespace SharpAssert.Demo.Demos;

public static class ObjectComparisonDemos
{
    record Person(string Name, int Age, string City);
    record Address(string Street, string City, string ZipCode);
    record Customer(string Name, Address Address);

    /// <summary>
    /// Demonstrates object comparison showing specific property differences.
    /// </summary>
    public static void SimplePropertyDifference()
    {
        var actual = new Person("Alice", 30, "New York");
        var expected = new Person("Alice", 25, "New York");
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates nested object comparison with deep property paths.
    /// </summary>
    public static void NestedObjectDifference()
    {
        var actual = new Customer(
            "Bob",
            new Address("123 Main St", "Boston", "02101"));
        var expected = new Customer(
            "Bob",
            new Address("123 Main St", "New York", "10001"));
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates null object comparison showing one is null.
    /// </summary>
    public static void NullObjectComparison()
    {
        Person? nullPerson = null;
        var nonNullPerson = new Person("Charlie", 35, "Chicago");
        Assert(nullPerson == nonNullPerson);
    }

    /// <summary>
    /// Demonstrates record type comparison with value equality.
    /// </summary>
    public static void RecordComparison()
    {
        var actual = new Person("David", 40, "Denver");
        var expected = new Person("David", 40, "Detroit");
        Assert(actual == expected);
    }

    /// <summary>
    /// Demonstrates object with multiple property differences.
    /// </summary>
    public static void MultiplePropertyDifferences()
    {
        var actual = new Person("Eve", 28, "Seattle");
        var expected = new Person("Eva", 30, "Portland");
        Assert(actual == expected);
    }
}
