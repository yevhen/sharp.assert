namespace SharpAssert;

static class AssertionFormatter
{
    public static string FormatAssertionFailure(AssertionContext context)
    {
        var locationPart = FormatLocation(context.File, context.Line);
        
        if (context.Message is not null)
            return $"{context.Message}\nAssertion failed: {context.Expression}  at {locationPart}";
        
        return $"Assertion failed: {context.Expression}  at {locationPart}";
    }
    
    public static string FormatLocation(string file, int line) =>
        $"{file}:{line}";
}