namespace SharpAssert;

static class AssertionFormatter
{
    public static string FormatLocation(string file, int line) => $"{file}:{line}";
}