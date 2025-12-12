namespace SharpAssert;

/// <summary>Provides call-site information to an <see cref="IExpectation"/> during evaluation.</summary>
/// <param name="Expression">The expression text captured at the call site.</param>
/// <param name="File">The source file path where the assertion was invoked.</param>
/// <param name="Line">The 1-based line number where the assertion was invoked.</param>
/// <param name="Message">An optional custom message supplied by the user.</param>
/// <param name="ExprNode">Expression text metadata used for precise rendering.</param>
public readonly record struct ExpectationContext(
    string Expression,
    string File,
    int Line,
    string? Message,
    ExprNode ExprNode);

