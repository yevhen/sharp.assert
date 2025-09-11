using System.Collections;
using KellermanSoftware.CompareNetObjects;

namespace SharpAssert;

class ObjectComparisonFormatter : IComparisonFormatter
{
    const int MaxObjectDifferences = 20; // Limit property differences shown
    
    public bool CanFormat(object? leftValue, object? rightValue)
    {
        // Only handle objects that are not strings or collections (already handled by other formatters)
        return IsObject(leftValue) || IsObject(rightValue);
    }

    public string FormatComparison(object? leftValue, object? rightValue)
    {
        if (leftValue == null && rightValue == null) 
            return string.Empty;
        if (leftValue == null) 
            return "  Left:  null\n  Right: " + FormatObjectType(rightValue);
        if (rightValue == null) 
            return "  Left:  " + FormatObjectType(leftValue) + "\n  Right: null";

        // Use CompareLogic to get detailed differences
        var compareLogic = new CompareLogic();
        compareLogic.Config.MaxDifferences = MaxObjectDifferences;

        var result = compareLogic.Compare(leftValue, rightValue);
        
        if (result.AreEqual)
            return string.Empty; // Objects are equal, should not have gotten here

        return FormatObjectDifferences(result.Differences);
    }

    static bool IsObject(object? value)
    {
        if (value == null) return true; // null can be compared to objects
        if (value is string) return false; // handled by StringComparisonFormatter
        if (value is IEnumerable) return false; // handled by CollectionComparisonFormatter
        if (value.GetType().IsPrimitive) return false; // primitives handled by DefaultComparisonFormatter
        if (value is DateTime or TimeSpan or DateTimeOffset or Guid or Enum) return false; // common value types
        
        return true; // Everything else is considered an object
    }

    static string FormatObjectType(object? value)
    {
        if (value == null) return "null";
        return value.GetType().Name;
    }

    static string FormatObjectDifferences(IList<Difference> differences)
    {
        var lines = new List<string>
        {
            "  Property differences:"
        };

        foreach (var diff in differences.Take(MaxObjectDifferences))
        {
            var propertyPath = diff.PropertyName;
            var expectedValue = FormatValue(diff.Object1Value);
            var actualValue = FormatValue(diff.Object2Value);
            
            lines.Add($"    {propertyPath}: expected {expectedValue}, got {actualValue}");
        }

        if (differences.Count > MaxObjectDifferences)
        {
            lines.Add($"    ... ({differences.Count - MaxObjectDifferences} more differences)");
        }

        return string.Join("\n", lines);
    }

    static string FormatValue(string? value)
    {
        if (value == null) return "null";
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return $"'{value}'";
    }
}