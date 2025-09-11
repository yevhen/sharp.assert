namespace SharpAssert;

class DefaultComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(object? leftValue, object? rightValue) => true;

    public string FormatComparison(object? leftValue, object? rightValue)
    {
        var leftDisplay = FormatValue(leftValue);
        var rightDisplay = FormatValue(rightValue);
        
        return $"  Left:  {leftDisplay}\n  Right: {rightDisplay}";
    }
    
    static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        _ => value.ToString()!
    };
}