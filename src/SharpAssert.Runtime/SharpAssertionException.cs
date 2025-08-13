namespace SharpAssert;

/// <summary>Exception thrown when an assertion fails.</summary>
public class SharpAssertionException(string message) : Exception(message);