using SharpAssert.Features.BinaryComparison;
using SharpAssert.Features.ObjectComparison;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

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

    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_show_property_differences()
        {
            var obj1 = new Person("Alice", 25);
            var obj2 = new Person("Bob", 30);
            
            var expected = BinaryComparison("obj1 == obj2", Equal, 
                ObjectComparison(obj1, obj2, 
                    Diff("Name", "Alice", "Bob"),
                    Diff("Age", "25", "30")));
            
            AssertFails(() => Assert(obj1 == obj2), expected);
        }

        [Test]
        public void Should_handle_nested_objects()
        {
            var obj1 = new PersonWithAddress("Alice", new Address("123 Main St", "NYC"));
            var obj2 = new PersonWithAddress("Alice", new Address("123 Main St", "LA"));

            var expected = BinaryComparison("obj1 == obj2", Equal,
                ObjectComparison(obj1, obj2,
                    Diff("Address.City", "NYC", "LA")));

            AssertFails(() => Assert(obj1 == obj2), expected);
        }

        [Test]
        public void Should_handle_null_objects()
        {
            Person? obj1 = null;
            var obj2 = new Person("Alice", 25);

            // Nulls handled by DefaultComparisonResult
            var expected = BinaryComparison("obj1 == obj2", Equal,
                DefaultComparison(obj1, obj2));

            AssertFails(() => Assert(obj1 == obj2), expected);
        }

        [Test]
        public void Should_pass_when_objects_are_equal()
        {
            var obj1 = new Person("Alice", 25);
            var obj2 = new Person("Alice", 25);
            AssertPasses(() => Assert(obj1 == obj2));
        }

        [Test]
        public void Should_respect_equality_overrides()
        {
            var obj1 = new PersonWithCustomEquals { Name = "Alice", Age = 25 };
            var obj2 = new PersonWithCustomEquals { Name = "Alice", Age = 30 };
            AssertPasses(() => Assert(obj1 == obj2));
        }

        [Test]
        public void Should_pass_when_nested_objects_are_equal()
        {
            var obj1 = new PersonWithAddress("Alice", new Address("123 Main St", "NYC"));
            var obj2 = new PersonWithAddress("Alice", new Address("123 Main St", "NYC"));
            AssertPasses(() => Assert(obj1 == obj2));
        }

        [Test]
        public void Should_pass_when_both_objects_are_null()
        {
            Person? obj1 = null;
            Person? obj2 = null;
            AssertPasses(() => Assert(obj1 == obj2));
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_differences()
        {
            var result = ObjectComparison(new object(), new object(),
                Diff("Name", "Alice", "Bob"),
                Diff("Age", "25", "30"));

            AssertRendersExactly(result,
                "Property differences:",
                "Name: expected \"Alice\", got \"Bob\"",
                "Age: expected \"25\", got \"30\"");
            // ValueFormatter quotes strings. CompareNetObjects values are strings.
            // So expected "Alice" -> "\"Alice\""
        }

        [Test]
        public void Should_render_truncated_message()
        {
            // Simulate truncation
            var result = new ObjectComparisonResult(
                Operand(new object()), 
                Operand(new object()), 
                [Diff("Prop1", "A", "B")], 
                TruncatedCount: 5);

            AssertRendersExactly(result,
                "Property differences:",
                "Prop1: expected \"A\", got \"B\"",
                "... (5 more differences)");
        }
    }

    static ObjectComparisonResult ObjectComparison(object left, object right, params ObjectDifference[] diffs) =>
        new(Operand(left), Operand(right), diffs, 0);

    static DefaultComparisonResult DefaultComparison(object? left, object? right) => new(Operand(left), Operand(right));

    static ObjectDifference Diff(string path, string expected, string actual) => new(path, expected, actual);
}
