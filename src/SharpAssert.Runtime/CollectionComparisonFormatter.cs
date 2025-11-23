using System.Collections;
using System.Linq;

namespace SharpAssert;

class CollectionComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(object? leftValue, object? rightValue) =>
        IsEnumerable(leftValue) && IsEnumerable(rightValue);

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        if (leftValue == null && rightValue == null)
        {
            return new ComparisonResult(
                new AssertionOperand(null, typeof(object)),
                new AssertionOperand(null, typeof(object)),
                Array.Empty<string>());
        }
        if (leftValue == null)
        {
            return new ComparisonResult(
                new AssertionOperand(leftValue, typeof(object)),
                new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
                new[] { "Left:  null", "Right: Collection" });
        }
        if (rightValue == null)
        {
            return new ComparisonResult(
                new AssertionOperand(leftValue, leftValue.GetType()),
                new AssertionOperand(rightValue, typeof(object)),
                new[] { "Left:  Collection", "Right: null" });
        }

        var leftList = MaterializeToList(leftValue);
        var rightList = MaterializeToList(rightValue);

        var lines = AnalyzeCollectionDifferences(leftList, rightList);
        return new ComparisonResult(
            new AssertionOperand(leftValue, leftValue.GetType()),
            new AssertionOperand(rightValue, rightValue.GetType()),
            lines);
    }

    static List<object?> MaterializeToList(object collection) =>
        ((IEnumerable)collection).Cast<object?>().ToList();

    static IReadOnlyList<string> AnalyzeCollectionDifferences(List<object?> left, List<object?> right)
    {
        var result = new List<string>
        {
            $"Left:  {FormatCollectionPreview(left)}",
            $"Right: {FormatCollectionPreview(right)}"
        };

        var differenceMessage = FindFirstDifference(left, right);
        if (differenceMessage != null)
            result.Add(differenceMessage);

        var lengthDifferenceMessage = DescribeLengthDifference(left, right);
        if (lengthDifferenceMessage != null)
            result.Add(lengthDifferenceMessage);

        return result;
    }

    static string? FindFirstDifference(List<object?> left, List<object?> right)
    {
        var minLength = Math.Min(left.Count, right.Count);
        for (var i = 0; i < minLength; i++)
        {
            if (!Equals(left[i], right[i]))
                return $"First difference at index {i}: expected {FormatValue(left[i])}, got {FormatValue(right[i])}";
        }
        return null;
    }

    static string? DescribeLengthDifference(List<object?> left, List<object?> right)
    {
        const int MaxDifferencePreview = 5;

        if (left.Count > right.Count)
        {
            var extraElements = left.Skip(right.Count).Take(MaxDifferencePreview).ToList();
            return $"Extra elements: [{string.Join(", ", extraElements.Select(FormatValue))}]";
        }

        if (right.Count > left.Count)
        {
            var missingElements = right.Skip(left.Count).Take(MaxDifferencePreview).ToList();
            return $"Missing elements: [{string.Join(", ", missingElements.Select(FormatValue))}]";
        }

        return null;
    }

    static string FormatCollectionPreview(List<object?> items)
    {
        const int maxPreview = 10;
        if (items.Count <= maxPreview)
            return $"[{string.Join(", ", items.Select(FormatValue))}]";

        var preview = items.Take(maxPreview - 1).Select(FormatValue);
        return $"[{string.Join(", ", preview)}, ... ({items.Count} items)]";
    }

    static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        DateTime dt => dt.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };

    static bool IsEnumerable(object? value) =>
        value is IEnumerable && value is not string;
}
