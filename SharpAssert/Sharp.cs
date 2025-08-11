using System.Runtime.CompilerServices;
using SharpAssert;

public static class Sharp
{
    /// <summary>Validates that a condition is true, throwing an exception with detailed error information if false.</summary>
    public static void Assert(
        bool condition,
        string? message = null,
        [CallerArgumentExpression("condition")] string? expr = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0)
    {
        if (message is not null && string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must be either null or non-empty", nameof(message));

        if (condition)
            return;

        var context = new AssertionContext(expr ?? "condition", file ?? "unknown", line, message);
        var formattedMessage = AssertionFormatter.FormatAssertionFailure(context);

        throw new SharpAssertionException(formattedMessage);
    }

    /// <summary>Asserts that a specific action throws an exception of type T, optionally providing a custom message.</summary>
    public static T AssertThrows<T>(Action action, string? message = null) where T : Exception
    {
        try
        {
            action();

            var failureMessage = $"Expected exception of type '{typeof(T).FullName}', but no exception was thrown";
            var finalMessage = message is not null
                ? $"{message}\n{failureMessage}"
                : failureMessage;

            throw new SharpAssertionException(finalMessage);
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            var exceptionDescription = string.IsNullOrEmpty(ex.Message) 
                ? ex.GetType().FullName 
                : $"{ex.GetType().FullName}: {ex.Message}";

            var failureMessage = $"Expected exception of type '{typeof(T).FullName}', but got '{exceptionDescription}'\n\nFull exception details:\n{ex}";

            var finalMessage = message is not null
                ? $"{message}\n{failureMessage}"
                : failureMessage;

            throw new SharpAssertionException(finalMessage);
        }
    }

    /// <summary>Asserts that a specific async action throws an exception of type T, optionally providing a custom message.</summary>
    public static async Task<T> AssertThrowsAsync<T>(Func<Task> action, string? message = null) where T : Exception
    {
        try
        {
            await action();

            var failureMessage = $"Expected exception of type '{typeof(T).FullName}', but no exception was thrown";
            var finalMessage = message is not null
                ? $"{message}\n{failureMessage}"
                : failureMessage;

            throw new SharpAssertionException(finalMessage);
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            // Handle AggregateException unwrapping (common in async scenarios)
            if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count == 1)
            {
                var innerEx = aggEx.InnerExceptions[0];
                if (innerEx is T expectedEx)
                    return expectedEx;
                
                // Use inner exception for error reporting
                ex = innerEx;
            }

            var exceptionDescription = string.IsNullOrEmpty(ex.Message) 
                ? ex.GetType().FullName 
                : $"{ex.GetType().FullName}: {ex.Message}";

            var failureMessage = $"Expected exception of type '{typeof(T).FullName}', but got '{exceptionDescription}'\n\nFull exception details:\n{ex}";

            var finalMessage = message is not null
                ? $"{message}\n{failureMessage}"
                : failureMessage;

            throw new SharpAssertionException(finalMessage);
        }
    }

    /// <summary>Asserts that a specific async function throws an exception of type T, optionally providing a custom message.</summary>
    public static async Task<T> AssertThrowsAsync<T>(Func<Task<object?>> action, string? message = null) where T : Exception
    {
        try
        {
            await action();

            var failureMessage = $"Expected exception of type '{typeof(T).FullName}', but no exception was thrown";
            var finalMessage = message is not null
                ? $"{message}\n{failureMessage}"
                : failureMessage;

            throw new SharpAssertionException(finalMessage);
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            // Handle AggregateException unwrapping (common in async scenarios)
            if (ex is AggregateException aggEx && aggEx.InnerExceptions.Count == 1)
            {
                var innerEx = aggEx.InnerExceptions[0];
                if (innerEx is T expectedEx)
                    return expectedEx;
                
                // Use inner exception for error reporting
                ex = innerEx;
            }

            var exceptionDescription = string.IsNullOrEmpty(ex.Message) 
                ? ex.GetType().FullName 
                : $"{ex.GetType().FullName}: {ex.Message}";

            var failureMessage = $"Expected exception of type '{typeof(T).FullName}', but got '{exceptionDescription}'\n\nFull exception details:\n{ex}";

            var finalMessage = message is not null
                ? $"{message}\n{failureMessage}"
                : failureMessage;

            throw new SharpAssertionException(finalMessage);
        }
    }

}