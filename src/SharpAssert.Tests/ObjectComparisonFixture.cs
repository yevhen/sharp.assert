using static SharpAssert.Sharp;

namespace SharpAssert;

[TestFixture]
public class ObjectComparisonFixture : TestBase
{
    record Person(string Name, int Age);
    record Address(string Street, string City);
    record PersonWithAddress(string Name, Address Address);

    class PersonWithCustomEquals
    {
        public string Name { get; init; } = "";
        public int Age { get; init; }
        
        public override bool Equals(object? obj) => 
            obj is PersonWithCustomEquals other && Name == other.Name;
            
        public override int GetHashCode() => Name.GetHashCode();
    }

    [Test]
    public void Should_pass_when_objects_are_equal()
    {
        var obj1 = new Person("Alice", 25);
        var obj2 = new Person("Alice", 25);

        AssertDoesNotThrow(() => Assert(obj1 == obj2));
    }

    [Test]
    public void Should_show_property_differences()
    {
        var obj1 = new Person("Alice", 25);
        var obj2 = new Person("Bob", 30);

        AssertThrows(() => Assert(obj1 == obj2),
            "*Property differences*Name*Alice*Bob*Age*25*30*");
    }

    [Test]
    public void Should_handle_nested_objects()
    {
        var obj1 = new PersonWithAddress("Alice", new Address("123 Main St", "NYC"));
        var obj2 = new PersonWithAddress("Alice", new Address("123 Main St", "LA"));

        AssertThrows(() => Assert(obj1 == obj2),
            "*Address.City*NYC*LA*");
    }

    [Test]
    public void Should_handle_null_objects()
    {
        Person? obj1 = null;
        var obj2 = new Person("Alice", 25);

        AssertThrows(() => Assert(obj1 == obj2),
            "*Left*null*Right*Person*");
    }

    [Test]
    public void Should_respect_equality_overrides()
    {
        var obj1 = new PersonWithCustomEquals { Name = "Alice", Age = 25 };
        var obj2 = new PersonWithCustomEquals { Name = "Alice", Age = 30 };

        AssertDoesNotThrow(() => Assert(obj1 == obj2));
    }

    [Test]
    public void Should_pass_when_nested_objects_are_equal()
    {
        var obj1 = new PersonWithAddress("Alice", new Address("123 Main St", "NYC"));
        var obj2 = new PersonWithAddress("Alice", new Address("123 Main St", "NYC"));

        AssertDoesNotThrow(() => Assert(obj1 == obj2));
    }

    [Test]
    public void Should_pass_when_both_objects_are_null()
    {
        Person? obj1 = null;
        Person? obj2 = null;

        AssertDoesNotThrow(() => Assert(obj1 == obj2));
    }
}