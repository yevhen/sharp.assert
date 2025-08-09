namespace SharpAssert;

/// <summary>Exception thrown when an assertion fails.</summary>
public class SharpAssertionException : Exception
{
    public SharpAssertionException(string message) : base(message)
    {
    }

    public SharpAssertionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}