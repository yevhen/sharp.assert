using FluentAssertions;
using SharpAssert.Core;
using SharpAssert.Features.Shared;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class EquivalencyFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_when_objects_equivalent()
        {
            var actual = new Person { Name = "John", Age = 30 };
            var expected = new Person { Name = "John", Age = 30 };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected)));
        }

        [Test]
        public void Should_fail_when_objects_differ()
        {
            var actual = new Person { Name = "John", Age = 30 };
            var expected = new Person { Name = "Jane", Age = 25 };

            var action = () => Assert(actual.IsEquivalentTo(expected));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_exclude_specified_property()
        {
            var actual = new Person { Name = "John", Age = 30, Id = 1 };
            var expected = new Person { Name = "John", Age = 30, Id = 2 };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config => config.Excluding(p => p.Id))));
        }

        [Test]
        public void Should_fail_when_non_excluded_properties_differ()
        {
            var actual = new Person { Name = "John", Age = 30, Id = 1 };
            var expected = new Person { Name = "Jane", Age = 30, Id = 2 };

            var action = () => Assert(actual.IsEquivalentTo(expected, config => config.Excluding(p => p.Id)));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_include_only_specified_property()
        {
            var actual = new Person { Name = "John", Age = 30, Id = 1 };
            var expected = new Person { Name = "Jane", Age = 25, Id = 1 };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config => config.Including(p => p.Id))));
        }

        [Test]
        public void Should_fail_when_included_property_differs()
        {
            var actual = new Person { Name = "John", Age = 30, Id = 1 };
            var expected = new Person { Name = "John", Age = 30, Id = 2 };

            var action = () => Assert(actual.IsEquivalentTo(expected, config => config.Including(p => p.Id)));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_ignore_collection_order_when_configured()
        {
            var actual = new Team { Name = "A", Members = ["Alice", "Bob", "Charlie"] };
            var expected = new Team { Name = "A", Members = ["Bob", "Charlie", "Alice"] };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config => config.WithoutStrictOrdering())));
        }

        [Test]
        public void Should_fail_when_collection_order_differs_by_default()
        {
            var actual = new Team { Name = "A", Members = ["Alice", "Bob", "Charlie"] };
            var expected = new Team { Name = "A", Members = ["Bob", "Charlie", "Alice"] };

            var action = () => Assert(actual.IsEquivalentTo(expected));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_both_objects_are_null()
        {
            Person? actual = null;
            Person? expected = null;

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected)));
        }

        [Test]
        public void Should_fail_when_actual_is_null_and_expected_is_not()
        {
            Person? actual = null;
            var expected = new Person { Name = "John", Age = 30 };

            var action = () => Assert(actual.IsEquivalentTo(expected));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_expected_is_null_and_actual_is_not()
        {
            var actual = new Person { Name = "John", Age = 30 };
            Person? expected = null;

            var action = () => Assert(actual.IsEquivalentTo(expected));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_handle_circular_references()
        {
            var person1 = new PersonWithSpouse { Name = "Alice", Age = 30 };
            var person2 = new PersonWithSpouse { Name = "Bob", Age = 32 };
            person1.Spouse = person2;
            person2.Spouse = person1;

            var expected1 = new PersonWithSpouse { Name = "Alice", Age = 30 };
            var expected2 = new PersonWithSpouse { Name = "Bob", Age = 32 };
            expected1.Spouse = expected2;
            expected2.Spouse = expected1;

            AssertPasses(() => Assert(person1.IsEquivalentTo(expected1)));
        }

        [Test]
        public void Should_fail_when_circular_objects_differ()
        {
            var person1 = new PersonWithSpouse { Name = "Alice", Age = 30 };
            var person2 = new PersonWithSpouse { Name = "Bob", Age = 32 };
            person1.Spouse = person2;
            person2.Spouse = person1;

            var expected1 = new PersonWithSpouse { Name = "Alice", Age = 30 };
            var expected2 = new PersonWithSpouse { Name = "Charlie", Age = 35 };
            expected1.Spouse = expected2;
            expected2.Spouse = expected1;

            var action = () => Assert(person1.IsEquivalentTo(expected1));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_compare_lists_with_same_order()
        {
            var actual = new CollectionContainer { Items = [1, 2, 3] };
            var expected = new CollectionContainer { Items = [1, 2, 3] };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected)));
        }

        [Test]
        public void Should_compare_lists_ignoring_order_when_configured()
        {
            var actual = new CollectionContainer { Items = [3, 1, 2] };
            var expected = new CollectionContainer { Items = [1, 2, 3] };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config => config.WithoutStrictOrdering())));
        }

        [Test]
        public void Should_fail_when_list_elements_differ()
        {
            var actual = new CollectionContainer { Items = [1, 2, 3] };
            var expected = new CollectionContainer { Items = [1, 2, 4] };

            var action = () => Assert(actual.IsEquivalentTo(expected));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_compare_empty_collections()
        {
            var actual = new CollectionContainer { Items = [] };
            var expected = new CollectionContainer { Items = [] };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected)));
        }

        [Test]
        public void Should_compare_anonymous_types()
        {
            var actual = new { Name = "Alice", Age = 30 };
            var expected = new { Name = "Alice", Age = 30 };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected)));
        }

        [Test]
        public void Should_fail_when_anonymous_type_properties_differ()
        {
            var actual = new { Name = "Alice", Age = 30 };
            var expected = new { Name = "Bob", Age = 30 };

            var action = () => Assert(actual.IsEquivalentTo(expected));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_use_custom_comparer_for_specific_type()
        {
            var actual = new OrderContainer { Order = new Order { Id = 1, Total = 100.0m } };
            var expected = new OrderContainer { Order = new Order { Id = 2, Total = 100.0m } };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config =>
                config.Using<Order>((a, b) => a.Total == b.Total))));
        }

        [Test]
        public void Should_fail_when_custom_comparer_returns_false()
        {
            var actual = new OrderContainer { Order = new Order { Id = 1, Total = 100.0m } };
            var expected = new OrderContainer { Order = new Order { Id = 1, Total = 200.0m } };

            var action = () => Assert(actual.IsEquivalentTo(expected, config =>
                config.Using<Order>((a, b) => a.Total == b.Total)));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_use_IEqualityComparer_for_specific_type()
        {
            var actual = new Team { Name = "Alpha", Members = ["ALICE", "BOB"] };
            var expected = new Team { Name = "Alpha", Members = ["alice", "bob"] };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config =>
                config.Using(StringComparer.OrdinalIgnoreCase))));
        }

        [Test]
        public void Should_fail_when_IEqualityComparer_indicates_not_equal()
        {
            var actual = new Team { Name = "Alpha", Members = ["ALICE", "BOB"] };
            var expected = new Team { Name = "Alpha", Members = ["alice", "bob"] };

            var action = () => Assert(actual.IsEquivalentTo(expected, config =>
                config.Using(StringComparer.Ordinal)));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_fields_excluded_and_only_fields_differ()
        {
            var actual = new ObjectWithField { Name = "Test" };
            actual.PublicField = "Value1";
            var expected = new ObjectWithField { Name = "Test" };
            expected.PublicField = "Value2";

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config => config.ExcludingFields())));
        }

        [Test]
        public void Should_fail_when_fields_included_and_fields_differ()
        {
            var actual = new ObjectWithField { Name = "Test" };
            actual.PublicField = "Value1";
            var expected = new ObjectWithField { Name = "Test" };
            expected.PublicField = "Value2";

            var action = () => Assert(actual.IsEquivalentTo(expected, config => config.IncludingFields()));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_fail_when_strict_ordering_and_order_differs()
        {
            var actual = new Team { Name = "A", Members = ["Alice", "Bob", "Charlie"] };
            var expected = new Team { Name = "A", Members = ["Bob", "Charlie", "Alice"] };

            var action = () => Assert(actual.IsEquivalentTo(expected, config => config.WithStrictOrdering()));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_when_strict_ordering_and_order_matches()
        {
            var actual = new Team { Name = "A", Members = ["Alice", "Bob"] };
            var expected = new Team { Name = "A", Members = ["Alice", "Bob"] };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config => config.WithStrictOrdering())));
        }

        [Test]
        public void Should_pass_when_recursion_disabled_and_nested_objects_differ()
        {
            var actual = new PersonWithSpouse { Name = "Alice", Age = 30 };
            actual.Spouse = new PersonWithSpouse { Name = "Bob", Age = 32 };
            var expected = new PersonWithSpouse { Name = "Alice", Age = 30 };
            expected.Spouse = new PersonWithSpouse { Name = "Charlie", Age = 40 };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config => config.WithoutRecursing())));
        }

        [Test]
        public void Should_still_compare_top_level_properties_when_recursion_disabled()
        {
            var actual = new PersonWithSpouse { Name = "Alice", Age = 30 };
            actual.Spouse = new PersonWithSpouse { Name = "Bob", Age = 32 };
            var expected = new PersonWithSpouse { Name = "Different", Age = 99 };
            expected.Spouse = new PersonWithSpouse { Name = "Bob", Age = 32 };

            var action = () => Assert(actual.IsEquivalentTo(expected, config => config.WithoutRecursing()));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_use_Equals_when_ComparingByValue()
        {
            var actual = new ContainerWithMoney { Amount = new Money(100, "USD") };
            var expected = new ContainerWithMoney { Amount = new Money(100, "USD") };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config =>
                config.ComparingByValue<Money>())));
        }

        [Test]
        public void Should_fail_when_Equals_returns_false()
        {
            var actual = new ContainerWithMoney { Amount = new Money(100, "USD") };
            var expected = new ContainerWithMoney { Amount = new Money(200, "USD") };

            var action = () => Assert(actual.IsEquivalentTo(expected, config =>
                config.ComparingByValue<Money>()));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_compare_enums_by_name_when_configured()
        {
            var actual = new StatusContainer { Status = StatusA.Active };
            var expected = new StatusContainer { Status = StatusB.Active };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config =>
                config.ComparingEnumsByName())));
        }

        [Test]
        public void Should_fail_when_enum_names_differ()
        {
            var actual = new StatusContainer { Status = StatusA.Active };
            var expected = new StatusContainer { Status = StatusB.Inactive };

            var action = () => Assert(actual.IsEquivalentTo(expected, config =>
                config.ComparingEnumsByName()));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_use_record_Equals_when_ComparingRecordsByValue()
        {
            var actual = new RecordContainer { Point = new Point(10, 20) };
            var expected = new RecordContainer { Point = new Point(10, 20) };

            AssertPasses(() => Assert(actual.IsEquivalentTo(expected, config =>
                config.ComparingRecordsByValue())));
        }

        [Test]
        public void Should_fail_when_record_Equals_returns_false()
        {
            var actual = new RecordContainer { Point = new Point(10, 20) };
            var expected = new RecordContainer { Point = new Point(30, 40) };

            var action = () => Assert(actual.IsEquivalentTo(expected, config =>
                config.ComparingRecordsByValue()));
            action.Should().Throw<SharpAssertionException>();
        }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_render_property_differences_clearly()
        {
            var actual = new Person { Id = 1, Name = "John", Age = 30 };
            var expected = new Person { Id = 1, Name = "Jane", Age = 25 };

            var expectation = actual.IsEquivalentTo(expected);
            var result = expectation.Evaluate(new ExpectationContext("test", "", 0, null, default));

            var rendered = Rendered(result);
            rendered.Should().Contain("Object differences:");
            rendered.Should().Contain("Name:");
        }
    }

    [TestFixture]
    class IntegrationTests
    {
        [Test]
        public void Should_compose_equivalency_with_other_expectations()
        {
            var person1 = new Person { Name = "John", Age = 30, Id = 1 };
            var person2 = new Person { Name = "John", Age = 30, Id = 1 };
            var expected = new Person { Name = "John", Age = 30, Id = 1 };

            AssertPasses(() => Assert(person1.IsEquivalentTo(expected) & person2.IsEquivalentTo(expected)));
        }

        [Test]
        public void Should_fail_composed_equivalency_when_one_fails()
        {
            var person1 = new Person { Name = "John", Age = 30, Id = 1 };
            var person2 = new Person { Name = "Jane", Age = 25, Id = 2 };
            var expected = new Person { Name = "John", Age = 30, Id = 1 };

            var action = () => Assert(person1.IsEquivalentTo(expected) & person2.IsEquivalentTo(expected));
            action.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_negate_equivalency_with_Not_operator()
        {
            var person = new Person { Name = "John", Age = 30, Id = 1 };
            var expected = new Person { Name = "Jane", Age = 25, Id = 2 };

            AssertPasses(() => Assert(!person.IsEquivalentTo(expected)));
        }

        [Test]
        public void Should_use_culture_invariant_formatting_for_dates()
        {
            var actual = new DateContainer { Date = new DateTime(2025, 12, 14) };
            var expected = new DateContainer { Date = new DateTime(2025, 12, 15) };

            var expectation = actual.IsEquivalentTo(expected);
            var result = expectation.Evaluate(new ExpectationContext("test", "", 0, null, default));

            var rendered = Rendered(result);
            rendered.Should().Contain("12/14/2025");
        }
    }

    class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    class Team
    {
        public string Name { get; set; } = "";
        public string[] Members { get; set; } = [];
    }

    class PersonWithSpouse
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public PersonWithSpouse? Spouse { get; set; }
    }

    class CollectionContainer
    {
        public List<int> Items { get; set; } = [];
    }

    class DateContainer
    {
        public DateTime Date { get; set; }
    }

    class Order
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
    }

    class OrderContainer
    {
        public Order Order { get; set; } = new();
    }

    class ObjectWithField
    {
        public string Name { get; set; } = "";
        public string PublicField = "";
    }

    class Money : IEquatable<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public bool Equals(Money? other)
        {
            if (other is null) return false;
            return Amount == other.Amount && Currency == other.Currency;
        }

        public override bool Equals(object? obj) => Equals(obj as Money);
        public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    }

    class ContainerWithMoney
    {
        public Money Amount { get; set; } = new(0, "USD");
    }

    enum StatusA { Active, Inactive }
    enum StatusB { Active = 100, Inactive = 200 }

    class StatusContainer
    {
        public Enum Status { get; set; } = StatusA.Active;
    }

    record Point(int X, int Y);

    class RecordContainer
    {
        public Point Point { get; set; } = new(0, 0);
    }
}
