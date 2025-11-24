using System.Collections;
using System.Linq;

namespace SharpAssert;

class CollectionComparer : IOperandComparer
{
    const int MaxPreview = 10;
    const int MaxDifferencePreview = 5;

    public bool CanCompare(object? leftValue, object? rightValue) =>
        IsEnumerable(leftValue) && IsEnumerable(rightValue);

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        if (leftValue == null)
        {
            return new CollectionComparisonResult(
                new AssertionOperand(leftValue, typeof(object)),
                new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
                Array.Empty<object?>(),
                Array.Empty<object?>(),
                null,
                null);
        }
        if (rightValue == null)
        {
            return new CollectionComparisonResult(
                new AssertionOperand(leftValue, leftValue.GetType()),
                new AssertionOperand(rightValue, typeof(object)),
                Array.Empty<object?>(),
                Array.Empty<object?>(),
                null,
                null);
        }

        var leftList = MaterializeToList(leftValue);
        var rightList = MaterializeToList(rightValue);

        var (firstDiff, lengthDiff, leftPreview, rightPreview) = AnalyzeCollectionDifferences(leftList, rightList);
        return new CollectionComparisonResult(
            new AssertionOperand(leftValue, leftValue.GetType()),
            new AssertionOperand(rightValue, rightValue.GetType()),
            leftPreview,
            rightPreview,
            firstDiff,
            lengthDiff);
    }

    static List<object?> MaterializeToList(object collection) =>
        ((IEnumerable)collection).Cast<object?>().ToList();

    static (CollectionMismatch? firstDiff, CollectionLengthDelta? lengthDiff, IReadOnlyList<object?> leftPreview, IReadOnlyList<object?> rightPreview) AnalyzeCollectionDifferences(List<object?> left, List<object?> right)
    {
        var differenceMessage = FindFirstDifference(left, right);

        var lengthDifferenceMessage = DescribeLengthDifference(left, right);

        return (differenceMessage, lengthDifferenceMessage, FormatCollectionPreview(left), FormatCollectionPreview(right));
    }

    static CollectionMismatch? FindFirstDifference(List<object?> left, List<object?> right)
    {
        var minLength = Math.Min(left.Count, right.Count);
        for (var i = 0; i < minLength; i++)
        {
            if (!Equals(left[i], right[i]))
                return new CollectionMismatch(i, left[i], right[i]);
        }
        return null;
    }

    static CollectionLengthDelta? DescribeLengthDifference(List<object?> left, List<object?> right)
    {
        if (left.Count > right.Count)
        {
            var extraElements = left.Skip(right.Count).Take(MaxDifferencePreview).ToList();
            return new CollectionLengthDelta(null, extraElements);
        }

        if (right.Count > left.Count)
        {
            var missingElements = right.Skip(left.Count).Take(MaxDifferencePreview).ToList();
            return new CollectionLengthDelta(missingElements, null);
        }

        return null;
    }

    static IReadOnlyList<object?> FormatCollectionPreview(List<object?> items)
    {
        if (items.Count <= MaxPreview)
            return items.ToArray();

        return items.Take(MaxPreview - 1).Concat(["..."]).ToArray();
    }

    static bool IsEnumerable(object? value) =>
        value is IEnumerable && value is not string;
}
