namespace SharpAssert;

[TestFixture]
public class LinqOperationsFixture : TestBase
{
    [TestFixture]
    public class PositiveTestCases
    {
        [Test]
        public void Should_pass_when_Contains_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertExpressionPasses(() => items.Contains(2));
        }

        [Test] 
        public void Should_pass_when_Any_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertExpressionPasses(() => items.Any(x => x > 1));
        }

        [Test]
        public void Should_pass_when_All_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertExpressionPasses(() => items.All(x => x > 0));
        }

        [Test]
        public void Should_pass_when_Any_succeeds_without_predicate()
        {
            var items = new[] { 1, 2, 3 };
            AssertExpressionPasses(() => items.Any());
        }
    }

    [TestFixture]
    public class FailureFormatting : TestBase
    {
        [Test]
        public void Should_show_collection_when_Contains_fails()
        {
            var items = new[] { 1, 2, 3 };
            
        AssertExpressionThrows<SharpAssertionException>(
                () => items.Contains(999),
                "items.Contains(999)",
                "LinqOperationsFixture.cs",
                42,
                "*Contains failed: searched for 999 in [1, 2, 3]*Count: 3*");
        }

        [Test]
        public void Should_show_matching_items_for_Any()
        {
            var items = new[] { 1, 2, 3 };
            
        AssertExpressionThrows<SharpAssertionException>(
                () => items.Any(x => x > 10),
                "items.Any(x => x > 10)",
                "LinqOperationsFixture.cs",
                42,
                "*Any failed: no items matched*x => (x > 10)*[1, 2, 3]*");
        }

        [Test]
        public void Should_show_failing_items_for_All()
        {
            var items = new[] { -1, 0, 1, 2 };
            
            AssertExpressionThrows(
                () => items.All(x => x > 0),
                "items.All(x => x > 0)",
                "LinqOperationsFixture.cs", 
                42,
                "*All failed: items [-1, 0] did not match*x => (x > 0)*");
        }

        [Test]
        public void Should_handle_empty_collections_in_LINQ()
        {
            var empty = Array.Empty<int>();
            
        AssertExpressionThrows<SharpAssertionException>(
                () => empty.Any(),
                "empty.Any()",
                "LinqOperationsFixture.cs",
                42,
                "*Any failed: collection is empty*");
        }
    }

    [TestFixture]
    public class CollectionTruncation : TestBase
    {
        [Test]
        public void Should_truncate_large_collections_in_Contains()
        {
            var items = Enumerable.Range(1, 15).ToArray(); // > 10 items
        AssertExpressionThrows<SharpAssertionException>(
                () => items.Contains(999),
                "items.Contains(999)",
                "LinqOperationsFixture.cs",
                42,
                "*[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...]*Count: 15*");
        }

        [Test]
        public void Should_truncate_large_collections_in_Any()
        {
            var items = Enumerable.Range(1, 15).ToArray();
        AssertExpressionThrows<SharpAssertionException>(
                () => items.Any(x => x > 20),
                "items.Any(x => x > 20)",
                "LinqOperationsFixture.cs",
                42,
                "*[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...]*");
        }

        [Test]
        public void Should_truncate_large_collections_in_All()
        {
            var items = Enumerable.Range(1, 15).ToArray();
            AssertExpressionThrows(
                () => items.All(x => x < 5),
                "items.All(x => x < 5)",
                "LinqOperationsFixture.cs",
                42,
                "*[5, 6, 7, 8, 9, 10, 11, 12, 13, 14, ...]*did not match*");
        }
    }

    [TestFixture]
    public class IEnumerableVsICollection : TestBase
    {
        [Test]
        public void Should_handle_IEnumerable_without_ICollection_Contains()
        {
            IEnumerable<int> enumerable = GetEnumerableOnly(); // LINQ query, not ICollection
        AssertExpressionThrows<SharpAssertionException>(
                () => enumerable.Contains(999),
                "enumerable.Contains(999)",
                "LinqOperationsFixture.cs",
                42,
                "*Count: 3*"); // Verifies fallback Count() logic
        }

        [Test]
        public void Should_handle_IEnumerable_without_ICollection_Any()
        {
            IEnumerable<int> enumerable = GetEnumerableOnly();
        AssertExpressionThrows<SharpAssertionException>(
                () => enumerable.Any(x => x > 10),
                "enumerable.Any(x => x > 10)",
                "LinqOperationsFixture.cs",
                42,
                "*no items matched*[1, 2, 3]*");
        }

        static IEnumerable<int> GetEnumerableOnly() => 
            new[] { 1, 2, 3 }.Where(x => true); // LINQ creates IEnumerable, not ICollection
    }

    [TestFixture]
    public class CustomObjectFormatting : TestBase
    {
        [Test]
        public void Should_format_custom_objects_in_Contains()
        {
            var items = new[] { new CustomObject("test") };
            AssertExpressionThrows<SharpAssertionException>(
                () => items.Contains(new CustomObject("missing")),
                "items.Contains(new CustomObject(\\\"missing\\\"))",
                "LinqOperationsFixture.cs",
                42,
                "*CustomObject(missing)*CustomObject(test)*");
        }

        [Test]
        public void Should_format_custom_objects_in_Any()
        {
            var items = new[] { new CustomObject("test1"), new CustomObject("test2") };
            AssertExpressionThrows<SharpAssertionException>(
                () => items.Any(x => x.Name == "missing"),
                "items.Any(x => x.Name == \\\"missing\\\")",
                "LinqOperationsFixture.cs",
                42,
                "*CustomObject(test1), CustomObject(test2)*");
        }

        [Test]
        public void Should_format_custom_objects_in_All()
        {
            var items = new[] { new CustomObject("pass"), new CustomObject("fail") };
            AssertExpressionThrows<SharpAssertionException>(
                () => items.All(x => x.Name == "pass"),
                "items.All(x => x.Name == \\\"pass\\\")",
                "LinqOperationsFixture.cs",
                42,
                "*CustomObject(fail)*did not match*");
        }

        record CustomObject(string Name)
        {
            public override string ToString() => $"CustomObject({Name})";
        }
    }

    [TestFixture] 
    public class AllEdgeCases : TestBase
    {
        [Test]
        public void Should_show_all_items_when_All_has_no_specific_failures()
        {
            var items = new[] { 1, 2, 3 };
            // Test case where all items fail the predicate
        AssertExpressionThrows<SharpAssertionException>(
                () => items.All(x => x == 0), // All fail the predicate
                "items.All(x => x == 0)",
                "LinqOperationsFixture.cs",
                42,
                "*All failed: items [1, 2, 3] did not match*");
        }
    }

    [TestFixture]
    public class StaticExtensionMethods : TestBase
    {
        [Test]
        public void Should_handle_static_extension_method_syntax()
        {
            var items = new[] { 1, 2, 3 };
        AssertExpressionThrows<SharpAssertionException>(
                () => Enumerable.Contains(items, 999),
                "Enumerable.Contains(items, 999)",
                "LinqOperationsFixture.cs",
                42,
                "*Contains failed: searched for 999*");
        }

        [Test]
        public void Should_handle_static_Any_syntax()
        {
            var items = new[] { 1, 2, 3 };
        AssertExpressionThrows<SharpAssertionException>(
                () => Enumerable.Any(items, x => x > 10),
                "Enumerable.Any(items, x => x > 10)",
                "LinqOperationsFixture.cs",
                42,
                "*Any failed: no items matched*");
        }

        [Test]
        public void Should_handle_static_All_syntax()
        {
            var items = new[] { -1, 0, 1, 2 };
        AssertExpressionThrows<SharpAssertionException>(
                () => Enumerable.All(items, x => x > 0),
                "Enumerable.All(items, x => x > 0)",
                "LinqOperationsFixture.cs",
                42,
                "*All failed: items [-1, 0] did not match*");
        }
    }
}