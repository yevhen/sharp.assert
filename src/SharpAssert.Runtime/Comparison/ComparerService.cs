namespace SharpAssert.Runtime.Comparison;

static class ComparerService
{
    static readonly IOperandComparer DefaultComparer = new DefaultComparer();

    static readonly IOperandComparer[] Comparers =
    [
        new NullableComparer(),
        new StringComparer(),
        new CollectionComparer(),
        new ObjectComparer(),
    ];

    public static ComparisonResult GetComparisonResult(AssertionOperand left, AssertionOperand right)
    {
        foreach (var comparer in Comparers)
        {
            if (comparer.CanCompare(left, right))
                return comparer.CreateComparison(left, right);
        }

        return DefaultComparer.CreateComparison(left, right);
    }
}
