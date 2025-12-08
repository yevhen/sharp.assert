using SharpAssert.Core;

namespace SharpAssert;

/// <summary>Exception thrown when an assertion fails.</summary>
public class SharpAssertionException : Exception
{
    public AssertionEvaluationResult? Result { get; }

    public SharpAssertionException(string message) : base(message)
    {
    }

    public SharpAssertionException(string message, AssertionEvaluationResult result) : base(message)
    {
        Result = result;
    }
}