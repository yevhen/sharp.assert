using System.Linq.Expressions;

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

        Expression<Func<bool>> expr = () => obj1 == obj2;

        AssertExpressionPasses(expr);
    }

    [Test]
    public void Should_show_property_differences()
    {
        var obj1 = new Person("Alice", 25);
        var obj2 = new Person("Bob", 30);
        
        Expression<Func<bool>> expr = () => obj1 == obj2;
        
        AssertExpressionThrows(
            expr, "obj1 == obj2", "test.cs", 42,
            "*Property differences*Name*Alice*Bob*Age*25*30*");
    }

    [Test]
    public void Should_handle_nested_objects()
    {
        var obj1 = new PersonWithAddress("Alice", new Address("123 Main St", "NYC"));
        var obj2 = new PersonWithAddress("Alice", new Address("123 Main St", "LA"));
        
        Expression<Func<bool>> expr = () => obj1 == obj2;
        
        AssertExpressionThrows(
            expr, "obj1 == obj2", "test.cs", 42,
            "*Address.City*NYC*LA*");
    }

    [Test]
    public void Should_handle_null_objects()
    {
        Person? obj1 = null;
        var obj2 = new Person("Alice", 25);
        
        Expression<Func<bool>> expr = () => obj1 == obj2;
        
        AssertExpressionThrows(
            expr, "obj1 == obj2", "test.cs", 42,
            "*Left*null*Right*Person*");
    }

    [Test]
    public void Should_respect_equality_overrides()
    {
        var obj1 = new PersonWithCustomEquals { Name = "Alice", Age = 25 };
        var obj2 = new PersonWithCustomEquals { Name = "Alice", Age = 30 };
        
        Expression<Func<bool>> expr = () => obj1 == obj2;
        
        AssertExpressionPasses(expr);
    }
}