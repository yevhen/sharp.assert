// ABOUTME: Tests for collection uniqueness expectation (AllUnique extension method).
// ABOUTME: Verifies duplicate detection logic and diagnostic formatting for collections.

using FluentAssertions;
using SharpAssert.Features.Collections;
using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class CollectionUniquenessFixture : TestBase
{
    [TestFixture]
    class LogicTests
    {
        [Test]
        public void Should_pass_for_unique_items()
        {
            var collection = new[] { 1, 2, 3, 4 };
            AssertPasses(() => Assert(collection.AllUnique()));
        }

        [Test]
        public void Should_fail_for_duplicates()
        {
            var collection = new[] { 1, 2, 3, 3 };
            var act = () => Assert(collection.AllUnique());
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_pass_for_empty_collection()
        {
            var collection = Array.Empty<int>();
            AssertPasses(() => Assert(collection.AllUnique()));
        }

        [Test]
        public void Should_pass_for_single_element()
        {
            var collection = new[] { 1 };
            AssertPasses(() => Assert(collection.AllUnique()));
        }

        [Test]
        public void Should_support_key_selector()
        {
            var users = new[]
            {
                new User { Name = "Alice", Email = "alice@example.com" },
                new User { Name = "Bob", Email = "bob@example.com" },
                new User { Name = "Charlie", Email = "charlie@example.com" }
            };

            AssertPasses(() => Assert(users.AllUnique(u => u.Email)));
        }

        [Test]
        public void Should_fail_with_key_selector_for_duplicates()
        {
            var users = new[]
            {
                new User { Name = "Alice", Email = "alice@example.com" },
                new User { Name = "Bob", Email = "bob@example.com" },
                new User { Name = "Alice Smith", Email = "alice@example.com" }
            };

            var act = () => Assert(users.AllUnique(u => u.Email));
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_handle_null_keys()
        {
            var users = new[]
            {
                new User { Name = "Alice", Email = null },
                new User { Name = "Bob", Email = "bob@example.com" },
                new User { Name = "Charlie", Email = null }
            };

            AssertPasses(() => Assert(users.AllUnique(u => u.Email)));
        }

        [Test]
        public void Should_detect_multiple_different_duplicates()
        {
            var collection = new[] { 1, 1, 2, 2, 3, 3 };
            var act = () => Assert(collection.AllUnique());
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_handle_all_duplicate_values()
        {
            var collection = new[] { 1, 1, 1, 1 };
            var act = () => Assert(collection.AllUnique());
            act.Should().Throw<SharpAssertionException>();
        }

        [Test]
        public void Should_support_operator_composition_with_AND()
        {
            var collection1 = new[] { 1, 2, 3 };
            var collection2 = new[] { 4, 5, 6 };

            AssertPasses(() => Assert(collection1.AllUnique() & collection2.AllUnique()));
        }

        [Test]
        public void Should_support_operator_composition_with_OR()
        {
            var collection1 = new[] { 1, 2, 2 };
            var collection2 = new[] { 4, 5, 6 };

            AssertPasses(() => Assert(collection1.AllUnique() | collection2.AllUnique()));
        }

        [Test]
        public void Should_support_operator_negation()
        {
            var collection = new[] { 1, 2, 2 };
            AssertPasses(() => Assert(!collection.AllUnique()));
        }
    }

    record User
    {
        public string Name { get; init; } = "";
        public string? Email { get; init; }
    }

    [TestFixture]
    class FormattingTests
    {
        [Test]
        public void Should_show_single_duplicate()
        {
            var collection = new[] { 1, 2, 3, 3 };
            var expectation = collection.AllUnique();
            var context = new ExpectationContext("collection.AllUnique()", "", 0, null, new ExprNode("collection.AllUnique()"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected all items to be unique, but item 3 is not unique.");
        }

        [Test]
        public void Should_show_multiple_duplicates()
        {
            var collection = new[] { 1, 2, 2, 3, 3 };
            var expectation = collection.AllUnique();
            var context = new ExpectationContext("collection.AllUnique()", "", 0, null, new ExprNode("collection.AllUnique()"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected all items to be unique, but items {2, 3} are not unique.");
        }

        [Test]
        public void Should_show_duplicate_strings()
        {
            var collection = new[] { "one", "two", "three", "three" };
            var expectation = collection.AllUnique();
            var context = new ExpectationContext("collection.AllUnique()", "", 0, null, new ExprNode("collection.AllUnique()"));
            var result = expectation.Evaluate(context);

            AssertRendersExactly(result,
                "False",
                "Expected all items to be unique, but item \"three\" is not unique.");
        }
    }
}
