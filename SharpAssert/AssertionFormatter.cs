namespace SharpAssert;

internal static class AssertionFormatter
{
    public static string FormatAssertionFailure(string? expr, string? file, int line)
    {
        var expressionPart = string.IsNullOrEmpty(expr) ? "false" : expr;
        var locationPart = FormatLocation(file, line);
        return $"Assertion failed: {expressionPart}  at {locationPart}";
    }
    
    public static string FormatLocation(string? file, int line)
    {
        return string.IsNullOrEmpty(file) ? $"line {line}" : $"{file}:{line}";
    }
}