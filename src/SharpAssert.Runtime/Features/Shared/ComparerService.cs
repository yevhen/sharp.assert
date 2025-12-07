using SharpAssert.Features.BinaryComparison;
using SharpAssert.Features.CollectionComparison;
using SharpAssert.Features.ObjectComparison;
using StringComparisonComparer = SharpAssert.Features.StringComparison.StringComparer;

namespace SharpAssert.Features.Shared;

static class ComparerService
{
    static readonly IOperandComparer DefaultComparer = new DefaultComparer();

    static readonly IOperandComparer[] Comparers =
    [
        new NullableComparer(),
        new StringComparisonComparer(),
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
