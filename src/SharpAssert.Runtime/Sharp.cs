using System.Collections;
using System.Runtime.CompilerServices;

namespace SharpAssert;

/// <summary>
/// Provides pytest-style assertions with detailed error diagnostics for .NET applications.
/// </summary>
/// <remarks>
/// <para>
/// SharpAssert automatically transforms assertion expressions at compile time using MSBuild source rewriting,
/// providing rich diagnostic information when assertions fail without requiring special syntax.
/// </para>
/// <para>
/// The library supports both synchronous and asynchronous exception testing, with intelligent fallback
/// to PowerAssert for complex scenarios to ensure comprehensive error reporting.
/// </para>
/// <para>
/// All assertion methods are thread-safe and can be used in concurrent testing scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using static SharpAssert.Sharp;
/// 
/// // Basic assertion with detailed diagnostics
/// var items = new[] { 1, 2, 3 };
/// var target = 4;
/// Assert(items.Contains(target));
/// // Failure: "items.Contains(target) - items: [1, 2, 3], target: 4, Result: false"
/// 
/// // Exception testing
/// var result = Throws&lt;ArgumentException&gt;(() => throw new ArgumentException("Invalid"));
/// Assert(result &amp;&amp; result.Message == "Invalid");
/// </code>
/// </example>
public static class Sharp
{
    /// <summary>
    /// Validates that a condition is true, throwing a detailed exception if the assertion fails.
    /// </summary>
    /// <param name="condition">The boolean condition to validate. Must be true for the assertion to pass.</param>
    /// <param name="message">
    /// Optional custom error message to include when the assertion fails. 
    /// Must be null or non-empty - empty strings and whitespace-only strings are rejected.
    /// </param>
    /// <param name="expr">
    /// The string representation of the condition expression. 
    /// Automatically populated by the compiler via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <param name="file">
    /// The source file path where the assertion was called.
    /// Automatically populated by the compiler via <see cref="CallerFilePathAttribute"/>.
    /// </param>
    /// <param name="line">
    /// The line number where the assertion was called.
    /// Automatically populated by the compiler via <see cref="CallerLineNumberAttribute"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="message"/> is an empty string or contains only whitespace.
    /// </exception>
    /// <exception cref="SharpAssertionException">
    /// Thrown when <paramref name="condition"/> is false, containing detailed diagnostic information
    /// about the failed assertion including expression text, variable values, and source location.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is enhanced by MSBuild source rewriting to provide rich diagnostic information.
    /// The rewriter transforms simple Assert calls into detailed analysis that shows variable values
    /// and expression evaluation steps when assertions fail.
    /// </para>
    /// <para>
    /// The method validates the message parameter to prevent accidental empty messages that provide
    /// no diagnostic value. Use null if no custom message is needed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage
    /// Assert(user.IsActive);
    /// 
    /// // With custom message
    /// Assert(items.Count > 0, "Collection should not be empty");
    /// 
    /// // Complex expressions with automatic diagnostics
    /// var expected = 42;
    /// var actual = GetValue();
    /// Assert(actual == expected);
    /// // Failure shows: "actual == expected - actual: 24, expected: 42, Result: false"
    /// </code>
    /// </example>
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

    /// <summary>
    /// Verifies that an action throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="T">The expected exception type that should be thrown by the action.</typeparam>
    /// <param name="action">The action to execute that is expected to throw an exception of type <typeparamref name="T"/>.</param>
    /// <returns>
    /// An <see cref="ExceptionResult{T}"/> that contains the caught exception and can be implicitly converted
    /// to <see cref="bool"/> (true if expected exception was thrown) or <typeparamref name="T"/> (the exception instance).
    /// </returns>
    /// <exception cref="SharpAssertionException">
    /// Thrown when the action throws an exception of a different type than <typeparamref name="T"/>.
    /// The exception message includes the expected type, actual type, and full stack trace details.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method executes the provided action and captures any exceptions thrown. If the exception
    /// is of the expected type <typeparamref name="T"/>, it returns a successful result containing the exception.
    /// If no exception is thrown, it returns a failed result. If an exception of a different type is thrown,
    /// it throws a <see cref="SharpAssertionException"/> with detailed diagnostic information.
    /// </para>
    /// <para>
    /// The returned <see cref="ExceptionResult{T}"/> can be used in assertions and provides convenient
    /// access to exception properties like Message and Data.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Verify specific exception type is thrown
    /// var result = Throws&lt;ArgumentException&gt;(() => 
    /// {
    ///     throw new ArgumentException("Invalid parameter");
    /// });
    /// Assert(result); // Passes because ArgumentException was thrown
    /// 
    /// // Check exception message
    /// Assert(result.Message == "Invalid parameter");
    /// 
    /// // Use in complex assertions
    /// Assert(Throws&lt;InvalidOperationException&gt;(() => service.Process()) &amp;&amp; 
    ///        service.State == ServiceState.Error);
    /// 
    /// // Verify no exception is thrown
    /// Assert(!Throws&lt;Exception&gt;(() => validOperation()));
    /// </code>
    /// </example>
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

    /// <summary>
    /// Verifies that an asynchronous action throws an exception of the specified type.
    /// </summary>
    /// <typeparam name="T">The expected exception type that should be thrown by the asynchronous action.</typeparam>
    /// <param name="action">
    /// The asynchronous action to execute that is expected to throw an exception of type <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// A task that resolves to an <see cref="ExceptionResult{T}"/> containing the caught exception.
    /// The result can be implicitly converted to <see cref="bool"/> (true if expected exception was thrown) 
    /// or <typeparamref name="T"/> (the exception instance).
    /// </returns>
    /// <exception cref="SharpAssertionException">
    /// Thrown when the action throws an exception of a different type than <typeparamref name="T"/>.
    /// The exception message includes the expected type, actual type, and full stack trace details.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method executes the provided asynchronous action and captures any exceptions thrown.
    /// It handles <see cref="AggregateException"/> unwrapping automatically, which is common in
    /// async scenarios where exceptions are wrapped.
    /// </para>
    /// <para>
    /// If the action throws an exception of the expected type <typeparamref name="T"/>, it returns
    /// a successful result. If no exception is thrown, it returns a failed result. If an exception
    /// of a different type is thrown, it throws a <see cref="SharpAssertionException"/>.
    /// </para>
    /// <para>
    /// The method is thread-safe and can be used in concurrent testing scenarios.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Verify async operation throws expected exception
    /// var result = await ThrowsAsync&lt;InvalidOperationException&gt;(async () => 
    /// {
    ///     await Task.Delay(10);
    ///     throw new InvalidOperationException("Async error");
    /// });
    ///
    /// Assert(result &amp;&amp; result.Message == "Async error");
    /// 
    /// // Test async service method
    /// var result = await ThrowsAsync&lt;ArgumentException&gt;(async () => 
    ///     await service.ProcessAsync(invalidData));
    ///
    /// Assert(result); // Passes if ArgumentException is thrown
    /// 
    /// // Verify no exception in async operation
    /// var result = await ThrowsAsync&lt;Exception&gt;(async () => 
    ///     await validAsyncOperation());
    ///
    /// Assert(!result); // Passes if no exception is thrown
    /// </code>
    /// </example>
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



    /// <summary>
    /// Represents the result of an exception testing operation, encapsulating whether the expected exception was thrown
    /// and providing convenient access to the exception details.
    /// </summary>
    /// <typeparam name="T">The type of exception that was expected to be thrown.</typeparam>
    /// <remarks>
    /// <para>
    /// This record provides implicit conversions to <see cref="bool"/> (indicating success/failure)
    /// and to the exception type <typeparamref name="T"/>, making it convenient to use in assertions
    /// and conditional logic.
    /// </para>
    /// <para>
    /// Use the boolean conversion to check if the expected exception was thrown, and use the exception
    /// conversion or <see cref="Exception"/> property to access the actual exception instance and its details.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = Throws&lt;ArgumentException&gt;(() => throw new ArgumentException("test"));
    /// 
    /// // Use as boolean - true if expected exception was thrown
    /// if (result) { /* exception was thrown as expected */ }
    /// 
    /// // Access exception directly via implicit conversion
    /// ArgumentException ex = result;
    /// 
    /// // Access exception properties
    /// Assert(result.Message == "test");
    /// Assert(result.Exception.GetType() == typeof(ArgumentException));
    /// </code>
    /// </example>
    public record ExceptionResult<T> where T: Exception
    {
        readonly T? exception;
        readonly bool success;

        internal ExceptionResult(T? exception, bool success)
        {
            this.exception = exception;
            this.success = success;
        }

        /// <summary>
        /// Implicitly converts an <see cref="ExceptionResult{T}"/> to a boolean indicating whether
        /// the expected exception was successfully caught.
        /// </summary>
        /// <param name="result">The exception result to convert.</param>
        /// <returns><c>true</c> if the expected exception was thrown; <c>false</c> if no exception was thrown.</returns>
        /// <example>
        /// <code>
        /// var result = Throws&lt;ArgumentException&gt;(() => ValidMethod());
        /// Assert(!result); // Passes if ValidMethod() doesn't throw ArgumentException
        /// </code>
        /// </example>
        public static implicit operator bool (ExceptionResult<T> result) => result.success;
        
        /// <summary>
        /// Implicitly converts an <see cref="ExceptionResult{T}"/> to the specific exception type.
        /// </summary>
        /// <param name="result">The exception result to convert.</param>
        /// <returns>The caught exception of type <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no exception was caught and the conversion is attempted.
        /// </exception>
        /// <example>
        /// <code>
        /// var result = Throws&lt;ArgumentException&gt;(() => throw new ArgumentException("test"));
        /// ArgumentException ex = result; // Implicit conversion to ArgumentException
        /// Assert(ex.Message == "test");
        /// </code>
        /// </example>
        public static implicit operator T(ExceptionResult<T> result) => result.Exception;
        
        /// <summary>
        /// Implicitly converts an <see cref="ExceptionResult{T}"/> to the base <see cref="Exception"/> type.
        /// </summary>
        /// <param name="result">The exception result to convert.</param>
        /// <returns>The caught exception as a base <see cref="Exception"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no exception was caught and the conversion is attempted.
        /// </exception>
        public static implicit operator Exception(ExceptionResult<T> result) => result.Exception;

        /// <summary>
        /// Gets the caught exception of the expected type.
        /// </summary>
        /// <value>The exception instance that was caught.</value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no exception was caught when this property is accessed.
        /// The exception message indicates the expected exception type and that no exception was thrown.
        /// </exception>
        /// <example>
        /// <code>
        /// var result = Throws&lt;ArgumentException&gt;(() => throw new ArgumentException("Invalid"));
        /// var ex = result.Exception;
        /// Assert(ex.Message == "Invalid");
        /// 
        /// // This would throw InvalidOperationException:
        /// var failedResult = Throws&lt;ArgumentException&gt;(() => { /* no exception */ });
        /// var ex = failedResult.Exception; // InvalidOperationException thrown here
        /// </code>
        /// </example>
        public T Exception => exception ?? throw new InvalidOperationException(
            $"Expected exception of type '{typeof(T).FullName}', but no exception was thrown");

        /// <summary>
        /// Gets the message from the caught exception.
        /// </summary>
        /// <value>The exception message, or throws if no exception was caught.</value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no exception was caught when this property is accessed.
        /// </exception>
        /// <example>
        /// <code>
        /// var result = Throws&lt;ArgumentException&gt;(() => throw new ArgumentException("Parameter invalid"));
        /// Assert(result.Message == "Parameter invalid");
        /// </code>
        /// </example>
        public string Message => Exception.Message;
        
        /// <summary>
        /// Gets the data dictionary from the caught exception.
        /// </summary>
        /// <value>
        /// The <see cref="IDictionary"/> containing additional data associated with the exception,
        /// or throws if no exception was caught.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no exception was caught when this property is accessed.
        /// </exception>
        /// <example>
        /// <code>
        /// var result = Throws&lt;ArgumentException&gt;(() => 
        /// {
        ///     var ex = new ArgumentException("Invalid parameter");
        ///     ex.Data["ParameterName"] = "userId";
        ///     throw ex;
        /// });
        /// 
        /// Assert(result.Data["ParameterName"].ToString() == "userId");
        /// </code>
        /// </example>
        public IDictionary Data => Exception.Data;
    }
}