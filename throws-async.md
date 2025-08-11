# AssertThrowsAsync Implementation Plan

## Overview
Add async exception testing support to SharpAssert, following NUnit's `ThrowsAsync` pattern while maintaining consistency with the existing `AssertThrows` implementation.

## Step-by-Step Implementation Plan

### Step 1: Add Method Signatures to Sharp.cs

Add these four overloads to the `Sharp` class, following the existing `AssertThrows` pattern:

```csharp
/// <summary>Asserts that a specific async action throws an exception of type T, optionally providing a custom message.</summary>
public static async Task<T> AssertThrowsAsync<T>(Func<Task> action, string? message = null) where T : Exception

/// <summary>Asserts that a specific async function throws an exception of type T, optionally providing a custom message.</summary>
public static async Task<T> AssertThrowsAsync<T>(Func<Task<object?>> action, string? message = null) where T : Exception

/// <summary>Asserts that a specific ValueTask action throws an exception of type T, optionally providing a custom message.</summary>
public static async Task<T> AssertThrowsAsync<T>(Func<ValueTask> action, string? message = null) where T : Exception

/// <summary>Asserts that a specific ValueTask function throws an exception of type T, optionally providing a custom message.</summary>
public static async Task<T> AssertThrowsAsync<T>(Func<ValueTask<object?>> action, string? message = null) where T : Exception
```

### Step 2: Implement Core Logic

Each method follows this pattern:

```csharp
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
```

### Step 3: Add Comprehensive Test Coverage

Add these tests to `CoreAssertionFixture.cs`:

#### Basic Functionality Tests

```csharp
[Test]
public async Task AssertThrowsAsync_catches_expected_exception_type()
{
    var exception = new InvalidOperationException("Test exception");

    var actual = await AssertThrowsAsync<InvalidOperationException>(async () => 
    {
        await Task.Delay(1);
        throw exception;
    });

    actual.Should().Be(exception);
}

[Test]
public async Task AssertThrowsAsync_returns_caught_exception()
{
    var actual = await AssertThrowsAsync<ArgumentException>(async () =>
    {
        await Task.Delay(1);
        throw new ArgumentException("Test message");
    });

    actual.Message.Should().Be("Test message");
    actual.Should().BeOfType<ArgumentException>();
}
```

#### Error Case Tests

```csharp
[Test]
public async Task AssertThrowsAsync_fails_when_no_exception_thrown()
{
    var actual = await Throws<SharpAssertionException>(async () =>
        await AssertThrowsAsync<ArgumentException>(async () => 
        {
            await Task.Delay(1);
            // No exception thrown
        }));

    actual!.Message.Should().Contain(
        "Expected exception of type 'System.ArgumentException', but no exception was thrown");
}

[Test]
public async Task AssertThrowsAsync_fails_when_wrong_exception_type()
{
    var actual = await Throws<SharpAssertionException>(async () =>
        await AssertThrowsAsync<ArgumentException>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Wrong exception");
        }));

    actual!.Message.Should().Contain(
        "Expected exception of type 'System.ArgumentException', " +
        "but got 'System.InvalidOperationException: Wrong exception'");
}

[Test]
public async Task AssertThrowsAsync_includes_custom_message_in_failures()
{
    var actual = await Throws<SharpAssertionException>(async () =>
        await AssertThrowsAsync<ArgumentException>(async () =>
        {
            await Task.Delay(1);
            // No exception thrown
        }, "Custom failure message"));

    actual!.Message.Should().Contain(
        "Custom failure message\nExpected exception of type 'System.ArgumentException', but no exception was thrown");
}
```

#### Stack Trace and Debugging Tests

```csharp
[Test]
public async Task AssertThrowsAsync_includes_full_stack_trace()
{
    var actual = await Throws<SharpAssertionException>(async () =>
        await AssertThrowsAsync<ArgumentException>(async () => await ThrowDeepAsyncException()));

    actual!.Message.Should().Contain("Full exception details:");
    actual.Message.Should().Contain("at SharpAssert.CoreAssertionFixture.ThrowDeepAsyncException()");
    actual.Message.Should().Contain("InvalidOperationException: Deep async exception");
}

async Task ThrowDeepAsyncException()
{
    await Task.Delay(1);
    throw new InvalidOperationException("Deep async exception with stack trace");
}
```

#### Async-Specific Scenario Tests

```csharp
[Test]
public async Task AssertThrowsAsync_handles_task_exceptions()
{
    var actual = await AssertThrowsAsync<TaskCanceledException>(async () =>
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await Task.Delay(1000, cts.Token);
    });

    actual.Should().BeOfType<TaskCanceledException>();
}

[Test]
public async Task AssertThrowsAsync_handles_valuetask_exceptions()
{
    var actual = await AssertThrowsAsync<ArgumentException>(async () =>
    {
        await new ValueTask(Task.Delay(1));
        throw new ArgumentException("ValueTask exception");
    });

    actual.Message.Should().Be("ValueTask exception");
}

[Test]
public async Task AssertThrowsAsync_unwraps_aggregate_exceptions()
{
    var actual = await AssertThrowsAsync<InvalidOperationException>(async () =>
    {
        var task = Task.Run(() => throw new InvalidOperationException("Inner exception"));
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            // Simulate AggregateException wrapping
            throw new AggregateException(ex);
        }
    });

    actual.Message.Should().Be("Inner exception");
}

[Test]
public async Task AssertThrowsAsync_works_with_task_returning_functions()
{
    var actual = await AssertThrowsAsync<ArgumentException>(async () =>
    {
        var result = await Task.FromResult("test");
        throw new ArgumentException($"Processing {result} failed");
    });

    actual.Message.Should().Be("Processing test failed");
}
```

#### Edge Case Tests

```csharp
[Test]
public async Task AssertThrowsAsync_handles_deep_async_call_stacks()
{
    var actual = await AssertThrowsAsync<InvalidOperationException>(async () =>
        await Level1Async());

    actual.Message.Should().Be("Level 3 exception");
    actual.Source.Should().NotBeNullOrEmpty();
}

async Task Level1Async()
{
    await Level2Async();
}

async Task Level2Async()
{
    await Level3Async();
}

async Task Level3Async()
{
    await Task.Delay(1);
    throw new InvalidOperationException("Level 3 exception");
}

[Test]
public async Task AssertThrowsAsync_preserves_async_context()
{
    var contextValue = "test-context";
    
    var actual = await AssertThrowsAsync<InvalidOperationException>(async () =>
    {
        await Task.Yield(); // Force async context switch
        throw new InvalidOperationException($"Context: {contextValue}");
    });

    actual.Message.Should().Contain("Context: test-context");
}
```

### Step 4: Add Helper Method for Async Throws Testing

Add this helper method to `CoreAssertionFixture.cs` for testing async assertion failures:

```csharp
// Helper method for testing async assertion failures
static async Task<T?> Throws<T>(Func<Task> action) where T : Exception
{
    try
    {
        await action();
        return null;
    }
    catch (T ex)
    {
        return ex;
    }
}
```

### Step 5: Update Documentation

Add comprehensive XML documentation to each method:

```csharp
/// <summary>
/// Asserts that a specific async action throws an exception of type T.
/// </summary>
/// <typeparam name="T">The expected exception type.</typeparam>
/// <param name="action">The async action that should throw an exception.</param>
/// <param name="message">Optional custom message to include in failure reports.</param>
/// <returns>The caught exception of type T for further inspection.</returns>
/// <exception cref="SharpAssertionException">
/// Thrown when no exception is thrown or when a different exception type is thrown.
/// </exception>
/// <example>
/// <code>
/// var ex = await AssertThrowsAsync&lt;ArgumentException&gt;(async () =&gt;
/// {
///     await SomeAsyncMethod();
/// });
/// Assert.That(ex.ParamName, Is.EqualTo("expectedParam"));
/// </code>
/// </example>
```

### Step 6: Testing Strategy

#### Unit Test Execution:
1. **Run existing tests**: `dotnet test SharpAssert.Tests/`
2. **Run specific fixture**: `dotnet test --filter "FullyQualifiedName~CoreAssertionFixture"`
3. **Run async tests only**: `dotnet test --filter "Name~AssertThrowsAsync"`

#### Integration Testing:
1. Test with real async scenarios (HTTP calls, database operations)
2. Verify performance characteristics
3. Test cancellation token behavior
4. Verify ConfigureAwait behavior

### Step 7: Error Handling Edge Cases

Handle these specific async scenarios:

1. **AggregateException Unwrapping**:
   - Single inner exception → unwrap and check type
   - Multiple inner exceptions → report AggregateException

2. **Task Cancellation**:
   - `OperationCanceledException` handled normally
   - `TaskCanceledException` handled normally

3. **Synchronous Exceptions**:
   - Exceptions thrown before first `await` are caught normally

4. **Task.Run Scenarios**:
   - Background task exceptions properly captured

### Step 8: Performance Considerations

1. **Minimal Overhead**: Use `ConfigureAwait(false)` where appropriate
2. **Memory Allocation**: Reuse error message formatting logic
3. **Exception Handling**: Efficient AggregateException unwrapping

### Step 9: API Consistency

Ensure consistency with existing `AssertThrows`:
- Same error message format
- Same parameter names and order  
- Same exception types thrown
- Same XML documentation style

### Step 10: Final Validation

Before completing implementation:

1. **All tests pass**: `dotnet test`
2. **Build succeeds**: `dotnet build`
3. **Documentation complete**: XML docs for all public methods
4. **Edge cases covered**: Comprehensive test coverage
5. **Performance acceptable**: No significant async overhead

## Implementation Benefits

### For Users:
- **Consistent API**: Matches existing `AssertThrows` patterns
- **Full debugging info**: Complete stack traces preserved
- **Modern async support**: Works with Task, ValueTask, and their generic variants  
- **NUnit compatibility**: Similar API for easy migration from NUnit

### For Maintainers:
- **Code reuse**: Shared error formatting logic with sync version
- **Comprehensive testing**: Prevents regressions in async scenarios
- **Clear documentation**: XML docs with practical examples
- **Future-proof**: Supports both Task and ValueTask patterns

## Files to Modify

1. **`/SharpAssert/Sharp.cs`**: Add the four `AssertThrowsAsync` method overloads
2. **`/SharpAssert.Tests/CoreAssertionFixture.cs`**: Add comprehensive async test coverage
3. **`/CLAUDE.md`**: Update with lessons learned during implementation

This implementation plan ensures robust async exception testing while maintaining SharpAssert's philosophy of providing detailed, actionable error messages for developers.