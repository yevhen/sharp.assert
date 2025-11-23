namespace SharpAssert;

class DefaultComparisonFormatter : IComparisonFormatter
{
    public bool CanFormat(object? leftValue, object? rightValue) => true;

    public ComparisonResult CreateComparison(object? leftValue, object? rightValue)
    {
        var lines = new[]
        {
            $"Left:  {FormatValue(leftValue)}",
            $"Right: {FormatValue(rightValue)}"
        };

        return new ComparisonResult(
            new AssertionOperand(leftValue, leftValue?.GetType() ?? typeof(object)),
            new AssertionOperand(rightValue, rightValue?.GetType() ?? typeof(object)),
            lines);
    }
    
    static string FormatValue(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s}\"",
        DateTime dt => dt.ToString("M/d/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };
}
