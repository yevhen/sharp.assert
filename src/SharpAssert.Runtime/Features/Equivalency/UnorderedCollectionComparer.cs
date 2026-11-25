// ABOUTME: Compares collections without regard to order by pairing each item with an equivalent one.
// ABOUTME: Matches items by full deep comparison and reports member differences of the items left without a pair.

using System.Collections;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;

namespace SharpAssert;

sealed class UnorderedCollectionComparer : BaseTypeComparer
{
    public UnorderedCollectionComparer() : base(RootComparerFactory.GetRootComparer()) { }

    public override bool IsTypeMatch(Type type1, Type type2) =>
        IsCollection(type1) && IsCollection(type2);

    public override void CompareType(CompareParms parms)
    {
        var unmatchedActual = new List<(object? item, int index)>();
        var unmatchedExpected = Items(parms.Object2).Select((item, index) => (item, index)).ToList();

        foreach (var (item, index) in Items(parms.Object1).Select((item, index) => (item, index)))
        {
            var match = unmatchedExpected.FindIndex(each => AreEquivalent(parms.Config, item, each.item));
            if (match >= 0)
                unmatchedExpected.RemoveAt(match);
            else
                unmatchedActual.Add((item, index));
        }

        foreach (var (actual, expected) in unmatchedActual.Zip(unmatchedExpected))
            CompareLeftovers(parms, actual.index, actual.item, expected.item);

        foreach (var (item, index) in unmatchedActual.Skip(unmatchedExpected.Count))
            AddUnmatched(parms, index, item, null);

        foreach (var (item, index) in unmatchedExpected.Skip(unmatchedActual.Count))
            AddUnmatched(parms, index, null, item);
    }

    void CompareLeftovers(CompareParms parms, int index, object? actual, object? expected) =>
        RootComparer.Compare(new CompareParms
        {
            Result = parms.Result,
            Config = parms.Config,
            ParentObject1 = parms.Object1,
            ParentObject2 = parms.Object2,
            Object1 = actual,
            Object2 = expected,
            BreadCrumb = $"{parms.BreadCrumb}[{index}]"
        });

    static bool IsCollection(Type type) =>
        typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    static List<object?> Items(object collection) =>
        ((IEnumerable)collection).Cast<object?>().ToList();

    static bool AreEquivalent(ComparisonConfig config, object? actual, object? expected) =>
        new CompareLogic(config).Compare(actual, expected).AreEqual;

    void AddUnmatched(CompareParms parms, int index, object? actual, object? expected) =>
        AddDifference(parms.Result, new Difference
        {
            ParentObject1 = parms.ParentObject1,
            ParentObject2 = parms.ParentObject2,
            PropertyName = $"{parms.BreadCrumb}[{index}]",
            Object1Value = NiceString(actual),
            Object2Value = NiceString(expected),
            Object1 = actual,
            Object2 = expected
        });
}
