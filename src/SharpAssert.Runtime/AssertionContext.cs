namespace SharpAssert;

record AssertionContext(string Expression, string File, int Line, string? Message);