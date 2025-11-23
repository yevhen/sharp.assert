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

        var lines = result.AreEqual ? Array.Empty<string>() : FormatObjectDifferences(result.Differences);

        return new ObjectComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(object)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
            lines);
    }

    static ComparisonResult? FormatNullComparison(object? leftValue, object? rightValue)
    {
        if (leftValue == null && rightValue == null)
        {
            return new ObjectComparisonResult(
                new AssertionOperand(null, typeof(object)),
                new AssertionOperand(null, typeof(object)),
                Array.Empty<string>());
        }

        if (leftValue == null)
        {
            return new ObjectComparisonResult(
                new AssertionOperand(null, typeof(object)),
                new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
                new[]
                {
                    "Left:  null",
                    "Right: " + FormatObjectValue(rightValue)
                });
        }

        if (rightValue == null)
        {
            return new ObjectComparisonResult(
                new AssertionOperand(leftValue, leftValue.GetType()),
                new AssertionOperand(null, typeof(object)),
                new[]
                {
                    "Left:  " + FormatObjectValue(leftValue),
                    "Right: null"
                });
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

    static string? FormatObjectValue(object? value)
    {
        if (value == null) return "null";

        var str = value.ToString();

        return IsDefaultToString(value, str) ? FormatObjectProperties(value) : str;
    }

    static bool IsDefaultToString(object value, string? str) =>
        str == value.GetType().ToString() || str == value.GetType().Name;

    static string FormatObjectProperties(object obj)
    {
        try
        {
            var properties = obj.GetType().GetProperties();
            if (properties.Length == 0)
                return obj.GetType().Name;

            var props = properties
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Take(MaxPropertiesDisplayed)
                .Select(p => $"{p.Name} = {p.GetValue(obj)}")
                .ToArray();

            return props.Length > 0 ? "{ " + string.Join(", ", props) + " }" : obj.GetType().Name;
        }
        catch
        {
            return obj.GetType().Name;
        }
    }

    static IReadOnlyList<string> FormatObjectDifferences(IList<Difference> differences)
    {
        var lines = new List<string>
        {
            "Property differences:"
        };

        lines.AddRange(
            from diff in differences.Take(MaxObjectDifferences)
            let propertyPath = diff.PropertyName
            let expectedValue = FormatValue(diff.Object1Value)
            let actualValue = FormatValue(diff.Object2Value)
            select $"  {propertyPath}: expected {expectedValue}, got {actualValue}");

        if (differences.Count > MaxObjectDifferences)
            lines.Add($"  ... ({differences.Count - MaxObjectDifferences} more differences)");

        return lines;
    }

    static string FormatValue(string? value)
    {
        if (value == null) return "null";
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return $"'{value}'";
    }
}
