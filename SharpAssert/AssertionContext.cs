namespace SharpAssert;

internal record AssertionContext(string Expression, string File, int Line, string? Message);