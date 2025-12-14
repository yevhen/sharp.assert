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
}
