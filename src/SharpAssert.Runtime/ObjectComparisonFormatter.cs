using System.Collections;
using System.Linq;
using KellermanSoftware.CompareNetObjects;

namespace SharpAssert;

class ObjectComparisonFormatter : IComparisonFormatter
{
    const int MaxObjectDifferences = 20;
    const int MaxPropertiesDisplayed = 5;
    
    public bool CanFormat(object? leftValue, object? rightValue) =>
        IsObject(leftValue) || IsObject(rightValue);

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        var nullResult = FormatNullComparison(leftValue, rightValue);
        if (nullResult != null)
            return nullResult;

        var compareLogic = new CompareLogic();
        compareLogic.Config.MaxDifferences = MaxObjectDifferences;

        var result = compareLogic.Compare(leftValue, rightValue);

        int truncated;
        IReadOnlyList<ObjectDifference> diffs;
        if (result.AreEqual)
        {
            diffs = Array.Empty<ObjectDifference>();
            truncated = 0;
        }
        else
        {
            diffs = FormatObjectDifferences(result.Differences, out truncated);
        }

        return new ObjectComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(object)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
            diffs,
            truncated);
    }

    static ComparisonResult? FormatNullComparison(object? leftValue, object? rightValue)
    {
        if (leftValue == null && rightValue == null)
            return new DefaultComparisonResult(new AssertionOperand(null, typeof(object)), new AssertionOperand(null, typeof(object)));

        if (leftValue == null)
        {
            return new DefaultComparisonResult(
                new AssertionOperand(null, typeof(object)),
                new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)));
        }

        if (rightValue == null)
        {
            return new DefaultComparisonResult(
                new AssertionOperand(leftValue, leftValue.GetType()),
                new AssertionOperand(null, typeof(object)));
        }
        return null;
    }

    static bool IsObject(object? value)
    {
        if (value == null) return true;
        if (value is string) return false;
        if (value is IEnumerable) return false;
        if (value.GetType().IsPrimitive) return false;
        if (value is DateTime or TimeSpan or DateTimeOffset or Guid or Enum) return false;

        return true;
    }

    static IReadOnlyList<ObjectDifference> FormatObjectDifferences(IList<Difference> differences, out int truncated)
    {
        truncated = Math.Max(0, differences.Count - MaxObjectDifferences);

        return differences
            .Take(MaxObjectDifferences)
            .Select(diff => new ObjectDifference(diff.PropertyName, diff.Object1Value, diff.Object2Value))
            .ToArray();
    }
}
