using static SharpAssert.Sharp;

namespace SharpAssert.Features;

[TestFixture]
public class LinqOperationsFixture : TestBase
{
    [TestFixture]
    public class PositiveTestCases : TestBase
    {
        [Test]
        public void Should_pass_when_Contains_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertDoesNotThrow(() => Assert(items.Contains(2)));
        }

        [Test]
        public void Should_pass_when_Any_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertDoesNotThrow(() => Assert(items.Any(x => x > 1)));
        }

        [Test]
        public void Should_pass_when_All_succeeds()
        {
            var items = new[] { 1, 2, 3 };
            AssertDoesNotThrow(() => Assert(items.All(x => x > 0)));
        }

        [Test]
        public void Should_pass_when_Any_succeeds_without_predicate()
        {
            var items = new[] { 1, 2, 3 };
            AssertDoesNotThrow(() => Assert(items.Any()));
        }
    }

    [TestFixture]
    public class FailureFormatting : TestBase
    {
        [Test]
        public void Should_show_collection_when_Contains_fails()
        {
            var items = new[] { 1, 2, 3 };
            AssertThrows(
                () => Assert(items.Contains(999)),
                "*Contains failed: searched for 999 in [1, 2, 3]*Count: 3*");
        }

        [Test]
        public void Should_show_matching_items_for_Any()
        {
            var items = new[] { 1, 2, 3 };
            AssertThrows(
                () => Assert(items.Any(x => x > 10)),
                "*Any failed: no items matched*x => (x > 10)*[1, 2, 3]*");
        }

        [Test]
        public void Should_show_failing_items_for_All()
        {
            var items = new[] { -1, 0, 1, 2 };
            AssertThrows(
                () => Assert(items.All(x => x > 0)),
                "*All failed: items [-1, 0] did not match*x => (x > 0)*");
        }

        [Test]
        public void Should_handle_empty_collections_in_LINQ()
        {
            var empty = Array.Empty<int>();
            AssertThrows(
                () => Assert(empty.Any()),
                "*Any failed: collection is empty*");
        }
    }

    [TestFixture]
    public class CollectionTruncation : TestBase
    {
        [Test]
        public void Should_truncate_large_collections_in_Contains()
        {
            var items = Enumerable.Range(1, 15).ToArray();
            AssertThrows(
                () => Assert(items.Contains(999)),
                "*[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...]*Count: 15*");
        }

        [Test]
        public void Should_truncate_large_collections_in_Any()
        {
            var items = Enumerable.Range(1, 15).ToArray();
            AssertThrows(
                () => Assert(items.Any(x => x > 20)),
                "*[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ...]*");
        }

        [Test]
        public void Should_truncate_large_collections_in_All()
        {
            var items = Enumerable.Range(1, 15).ToArray();
            AssertThrows(
                () => Assert(items.All(x => x < 5)),
                "*[5, 6, 7, 8, 9, 10, 11, 12, 13, 14, ...]*did not match*");
        }
    }

    [TestFixture]
    public class IEnumerableVsICollection : TestBase
    {
        [Test]
        public void Should_handle_IEnumerable_without_ICollection_Contains()
        {
            IEnumerable<int> enumerable = GetEnumerableOnly();
            AssertThrows(
                () => Assert(enumerable.Contains(999)),
                "*Count: 3*");
        }

        [Test]
        public void Should_handle_IEnumerable_without_ICollection_Any()
        {
            IEnumerable<int> enumerable = GetEnumerableOnly();
            AssertThrows(
                () => Assert(enumerable.Any(x => x > 10)),
                "*no items matched*[1, 2, 3]*");
        }

        static IEnumerable<int> GetEnumerableOnly() =>
            new[] { 1, 2, 3 }.Where(x => true);
    }

    [TestFixture]
    public class CustomObjectFormatting : TestBase
    {
        [Test]
        public void Should_format_custom_objects_in_Contains()
        {
            var items = new[] { new CustomObject("test") };
            AssertThrows(
                () => Assert(items.Contains(new CustomObject("missing"))),
                "*CustomObject(missing)*CustomObject(test)*");
        }

        [Test]
        public void Should_format_custom_objects_in_Any()
        {
            var items = new[] { new CustomObject("test1"), new CustomObject("test2") };
            AssertThrows(
                () => Assert(items.Any(x => x.Name == "missing")),
                "*CustomObject(test1), CustomObject(test2)*");
        }

        [Test]
        public void Should_format_custom_objects_in_All()
        {
            var items = new[] { new CustomObject("pass"), new CustomObject("fail") };
            AssertThrows(
                () => Assert(items.All(x => x.Name == "pass")),
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
            AssertThrows(
                () => Assert(items.All(x => x == 0)),
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
            AssertThrows(
                () => Assert(items.Contains(999)),
                "*Contains failed: searched for 999*");
        }

        [Test]
        public void Should_handle_static_Any_syntax()
        {
            var items = new[] { 1, 2, 3 };
            AssertThrows(
                () => Assert(items.Any(x => x > 10)),
                "*Any failed: no items matched*");
        }

        [Test]
        public void Should_handle_static_All_syntax()
        {
            var items = new[] { -1, 0, 1, 2 };
            AssertThrows(
                () => Assert(items.All(x => x > 0)),
                "*All failed: items [-1, 0] did not match*");
        }
    }
}