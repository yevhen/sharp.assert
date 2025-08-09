namespace SharpAssert;

internal static class AssertionFormatter
{
    public static string FormatAssertionFailure(string? expr, string? file, int line)
    {
        var locationPart = FormatLocation(file, line);
        return $"Assertion failed: {expr}  at {locationPart}";
    }
    
    public static string FormatLocation(string? file, int line) =>
        $"{file}:{line}";
}