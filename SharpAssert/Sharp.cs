using System.Collections;
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

    public static ExceptionResult<T> Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();

            return new ExceptionResult<T>(null, false); // Return a failure result with no exception
        }
        catch (T ex)
        {
            return new ExceptionResult<T>(ex, true);
        }
        catch (Exception ex)
        {
            throw new SharpAssertionException(
                $"Expected exception of type '{typeof(T).FullName}', " +
                $"but got '{ex.GetType().FullName}': {ex.Message}\n" +
                $"Full exception details:\n{ex}");
        }
    }

    public static async Task<ExceptionResult<T>> ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try
        {
            await action();

            return new ExceptionResult<T>(null, false); // Return a failure result with no exception
        }
        catch (T ex)
        {
            return new ExceptionResult<T>(ex, true);
        }
        catch (Exception ex)
        {
            // Handle AggregateException unwrapping (common in async scenarios)
            if (ex is AggregateException { InnerExceptions.Count: 1 } aggregateException)
            {
                var innerException = aggregateException.InnerExceptions[0];
                if (innerException is T expectedException)
                    return new ExceptionResult<T>(expectedException, true);

                // Use inner exception for error reporting
                ex = innerException;
            }

            throw new SharpAssertionException(
                $"Expected exception of type '{typeof(T).FullName}', " +
                $"but got '{ex.GetType().FullName}': {ex.Message}\n" +
                $"Full exception details:\n{ex}");
        }
    }



    public record ExceptionResult<T> where T: Exception
    {
        readonly T? exception;
        readonly bool success;

        internal ExceptionResult(T? exception, bool success)
        {
            this.exception = exception;
            this.success = success;
        }

        public static implicit operator bool (ExceptionResult<T> result) => result.success;
        public static implicit operator T(ExceptionResult<T> result) => result.Exception;
        public static implicit operator Exception(ExceptionResult<T> result) => result.Exception;

        public T Exception => exception ?? throw new InvalidOperationException(
            $"Expected exception of type '{typeof(T).FullName}', but no exception was thrown");

        public string Message => Exception.Message;
        public IDictionary Data => Exception.Data;
    }
}