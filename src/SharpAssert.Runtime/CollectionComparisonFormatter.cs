using System.Collections;

namespace SharpAssert;

class CollectionComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(object? leftValue, object? rightValue) =>
        IsEnumerable(leftValue) && IsEnumerable(rightValue);

    public string FormatComparison(object? leftValue, object? rightValue)
    {
        if (leftValue == null && rightValue == null) return string.Empty;
        if (leftValue == null) return "  Left:  null\n  Right: Collection";
        if (rightValue == null) return "  Left:  Collection\n  Right: null";

        var leftList = MaterializeToList(leftValue);
        var rightList = MaterializeToList(rightValue);

        return AnalyzeCollectionDifferences(leftList, rightList);
    }

    static List<object?> MaterializeToList(object collection)
    {
        var result = new List<object?>();
        foreach (var item in (IEnumerable)collection)
        {
            result.Add(item);
        }
        return result;
    }

    static string AnalyzeCollectionDifferences(List<object?> left, List<object?> right)
    {
        var result = new List<string>();
        
        // Basic left/right display
        result.Add($"  Left:  {FormatCollectionPreview(left)}");
        result.Add($"  Right: {FormatCollectionPreview(right)}");
        
        // Find first difference
        var minLength = Math.Min(left.Count, right.Count);
        for (var i = 0; i < minLength; i++)
        {
            if (!Equals(left[i], right[i]))
            {
                result.Add($"  First difference at index {i}: expected {FormatValue(left[i])}, got {FormatValue(right[i])}");
                return string.Join("\n", result);
            }
        }
        
        // Handle length differences
        if (left.Count > right.Count)
        {
            var extraElements = left.Skip(right.Count).Take(5).ToList();
            result.Add($"  Extra elements: [{string.Join(", ", extraElements.Select(FormatValue))}]");
        }
        else if (right.Count > left.Count)
        {
            var missingElements = right.Skip(left.Count).Take(5).ToList();
            result.Add($"  Missing elements: [{string.Join(", ", missingElements.Select(FormatValue))}]");
        }
        
        return string.Join("\n", result);
    }

    static string FormatCollectionPreview(List<object?> items)
    {
        const int maxPreview = 10;
        if (items.Count <= maxPreview)
        {
            return $"[{string.Join(", ", items.Select(FormatValue))}]";
        }
        
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