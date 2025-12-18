<p align="center">
  <img src="https://raw.githubusercontent.com/yevhen/sharp.assert/refs/heads/main/logo.png" alt="SharpAssert logo"/>
</p>

# SharpAssert

A pytest inspired assertion library for .NET with no special syntax.

## Overview

SharpAssert provides rich assertion diagnostics by automatically transforming your assertion expressions at compile time 
using MSBuild source rewriting, giving you detailed failure messages with powerful expression analysis.

```csharp
using static SharpAssert.Sharp;

var items = new[] { 1, 2, 3 };
var target = 4;

Assert(items.Contains(target));
// Assertion failed: items.Contains(target) at MyTest.cs:15
// items:  [1, 2, 3]  
// target: 4
// Result: false
```

## How It Works

SharpAssert uses **MSBuild source rewriting** to automatically transform your assertion calls at compile time:

1. **You write:** `Assert(x == y)`
2. **MSBuild rewrites:** `global::SharpAssert.SharpInternal.Assert(() => x == y, "x == y", "file.cs", 42)`
3. **Runtime analysis:** Expression tree provides detailed failure diagnostics when assertions fail

## Features

- **🔍 [Detailed Expression Analysis](#complex-expression-analysis)** - See exactly why your assertions failed
- **🎯 [Exception Testing](#exception-testing)** - `Throws<T>` and `ThrowsAsync<T>` with detailed exception diagnostics
- **🔤 [String Diffs](#string-comparisons)** - Character-level inline diffs for strings (powered by DiffPlex)
- **🔠 [String Pattern Matching](#string-pattern-matching)** - Wildcard patterns and occurrence counting
- **📊 [Collection Comparison](#collection-comparisons)** - First mismatch, missing/extra elements detection
- **📋 [Collection Ordering](#collection-ordering)** - Ascending/descending order validation
- **🔢 [Collection Uniqueness](#collection-uniqueness)** - Duplicate detection with key selectors
- **🔎 [Object Deep Diff](#object-deep-comparison)** - Property-level differences for objects/records (powered by Compare-Net-Objects)
- **🔄 [Object Equivalency](#object-equivalency)** - Structural comparison with property exclusion/inclusion
- **🔗 [LINQ Operations](#linq-operations)** - Enhanced diagnostics for Contains/Any/All operations
- **⚡ [Async/Await Support](#asyncawait-support)** - Full support for async assertions with value diagnostics
- **💫 Dynamic Types** - Dynamic objects support (Expando)

## Live Examples

See [demo](src/SharpAssert.Demo/demo.md) for assertion example output.

## Quick Start

### 1. Install Package

```bash
dotnet add package SharpAssert
```

### 2. Using SharpAssert

```csharp
using static SharpAssert.Sharp;

[Test]
public void Should_be_equal()
{
    var expected = 4;
    var actual = 5;
    
    Assert(expected == actual);
    // Assertion failed: expected == actual
    // Left: 4
    // Right: 5  
    // Result: false
}
```

## Features in Detail

### String Comparisons

Character-level diffs powered by DiffPlex:

```csharp
var actual = "hello";
var expected = "hallo";

Assert(actual == expected);
// Assertion failed: actual == expected
// String diff (inline):
//   h[-e][+a]llo
```

Multiline string diffs:

```csharp
var actual = "line1\nline2\nline3";
var expected = "line1\nMODIFIED\nline3";

Assert(actual == expected);
// Assertion failed: actual == expected
// String diff:
//   line1
// - line2
// + MODIFIED
//   line3
```

### Collection Comparisons

First mismatch and missing/extra elements:

```csharp
var actual = new[] { 1, 2, 3, 5 };
var expected = new[] { 1, 2, 4, 5 };

Assert(actual.SequenceEqual(expected));
// Assertion failed: actual.SequenceEqual(expected)
// Collections differ at index 2:
//   Expected: 4
//   Actual:   3
```

### String Pattern Matching

Wildcard patterns with `*` (any sequence) and `?` (single character):

```csharp
using SharpAssert.Features.Strings;

Assert("hello world".Matches("hello *"));      // * matches any sequence
Assert("test.txt".Matches("*.txt"));           // File extension matching
Assert("test".Matches("t?st"));                // ? matches single character
Assert("HELLO".MatchesIgnoringCase("hello"));  // Case-insensitive
```

Count substring occurrences:

```csharp
Assert("error at line 5, error at line 10".Contains("error", Occur.Exactly(2)));
Assert("warn, warn, warn".Contains("warn", Occur.AtLeast(2)));
Assert("info".Contains("info", Occur.AtMost(1)));
```

Regex pattern occurrence counting:

```csharp
Assert("test123 and test456".MatchesRegex(@"test\d+", Occur.Exactly(2)));
```

### Collection Ordering

Validate collections are sorted:

```csharp
using SharpAssert.Features.Collections;

var ascending = new[] { 1, 2, 3, 4 };
Assert(ascending.IsInAscendingOrder());

var descending = new[] { 4, 3, 2, 1 };
Assert(descending.IsInDescendingOrder());

// With custom comparer
Assert(names.IsInAscendingOrder(StringComparer.OrdinalIgnoreCase));
```

### Collection Uniqueness

Validate collections contain no duplicates:

```csharp
using SharpAssert.Features.Collections;

var unique = new[] { 1, 2, 3, 4 };
Assert(unique.AllUnique());

// Uniqueness by property
var users = new[] { user1, user2, user3 };
Assert(users.AllUnique(u => u.Email));

// Custom equality comparer
Assert(items.AllUnique(StringComparer.OrdinalIgnoreCase));
```

### Object Deep Comparison

Property-level diffs powered by Compare-Net-Objects:

```csharp
var actual = new User { Name = "John", Age = 30, City = "NYC" };
var expected = new User { Name = "John", Age = 25, City = "LA" };

Assert(actual == expected);
// Assertion failed: actual == expected
// Object differences:
//   Age: 30 → 25
//   City: "NYC" → "LA"
```

### Object Equivalency

Structural comparison with fluent configuration:

```csharp
var actual = new Person { Name = "John", Age = 30, Id = 1 };
var expected = new Person { Name = "John", Age = 30, Id = 2 };

// Basic equivalency check
Assert(actual.IsEquivalentTo(expected));

// Exclude specific properties
Assert(actual.IsEquivalentTo(expected, config => config.Excluding(p => p.Id)));

// Include only specific properties
Assert(actual.IsEquivalentTo(expected, config => config.Including(p => p.Name)));

// Ignore collection ordering
var team1 = new Team { Members = new[] { "Alice", "Bob" } };
var team2 = new Team { Members = new[] { "Bob", "Alice" } };
Assert(team1.IsEquivalentTo(team2, config => config.WithoutStrictOrdering()));
```

### LINQ Operations

Enhanced diagnostics for Contains, Any, All:

```csharp
var users = new[] { "Alice", "Bob", "Charlie" };

Assert(users.Contains("David"));
// Assertion failed: users.Contains("David")
// Collection: ["Alice", "Bob", "Charlie"]
// Looking for: "David"
// Result: false
```

### Async/Await Support

Full support for async expressions:

```csharp
Assert(await client.GetAsync() == await server.GetAsync());
// Assertion failed: await client.GetAsync() == await server.GetAsync()
// Left:  { Id: 1, Name: "Client" }
// Right: { Id: 2, Name: "Server" }
// Result: false
```

### Exception Testing

Test expected exceptions with `Throws<T>` and `ThrowsAsync<T>`:

```csharp
// Positive assertion - expects exception
Assert(Throws<ArgumentException>(() => throw new ArgumentException("invalid")));

// Negative assertion - expects no exception
Assert(!Throws<ArgumentException>(() => { /* no exception */ }));

// Access exception properties
var ex = Throws<ArgumentNullException>(() => throw new ArgumentNullException("param"));
Assert(ex.Message.Contains("param"));

// Async version
Assert(await ThrowsAsync<InvalidOperationException>(() =>
    Task.Run(() => throw new InvalidOperationException())));
```

### Custom Expectations

Create reusable expectations using `Expectation.From()` - a lambda-based factory that eliminates boilerplate:

```csharp
public static class NumberExtensions
{
    public static Expectation IsEven(this int value) =>
        Expectation.From(
            () => value % 2 == 0,
            () => [$"Expected even number, got {value}"]
        );

    public static Expectation IsBetween(this int value, int min, int max) =>
        Expectation.From(
            () => value >= min && value <= max,
            () => [$"Expected {value} to be between {min} and {max}"]
        );
}

Assert(4.IsEven());
Assert(!5.IsEven());
Assert(5.IsBetween(1, 10));
```

**How it works:**
- First lambda: predicate that returns `true` if expectation passes
- Second lambda: failure message factory (only called on failure, returns `string[]` for multiple diagnostic lines)
- Expression text is injected automatically by the framework

For **complex expectations** with substantial internal logic (algorithms, external library integration), you can still inherit from `Expectation`:

```csharp
sealed class IsEquivalentToExpectation<T>(T actual, T expected) : Expectation
{
    public override EvaluationResult Evaluate(ExpectationContext context)
    {
        // Complex comparison logic using Compare-Net-Objects...
        return isEqual
            ? ExpectationResults.Pass(context.Expression)
            : ExpectationResults.Fail(context.Expression, differences);
    }
}
```

### Custom Error Messages

```csharp
Assert(user.IsActive, $"User {user.Name} should be active for this operation");
```

### Complex Expression Analysis

```csharp
var order = new Order { Items = new[] { "Coffee", "Tea" }, Total = 15.50m };
var expectedTotal = 12.00m;

Assert(order.Items.Length > 0 && order.Total == expectedTotal);
// Assertion failed: order.Items.Length > 0 && order.Total == expectedTotal
// order.Items.Length > 0 → True (Length: 2)
// order.Total == expectedTotal → False
//   order.Total:  15.50
//   expectedTotal: 12.00
// Result: False
```

## Architecture

SharpAssert is built on modern .NET technologies:

- **MSBuild Source Rewriting** - Compile-time code transformation
- **Roslyn Syntax Analysis** - Advanced C# code parsing and generation
- **Expression Trees** - Runtime expression analysis for rich diagnostics
- **DiffPlex** - String and sequence diffs
- **CompareNETObjects** - Deep object comparison
- **CallerArgumentExpression** - Fallback for edge cases

### Requirements
- .NET 9.0 or later
- Test frameworks: xUnit, NUnit, or MSTest

### Packages
SharpAssert consists of two NuGet packages:

- **SharpAssert** - Main package with MSBuild rewriter (install this one)
- **SharpAssert.Runtime** - Core assertion library (automatically included as dependency)

When you install `SharpAssert`, you get everything you need. The runtime package is a transitive dependency and requires no separate installation.

## Configuration

### Diagnostic Logging

Enable detailed rewriter diagnostics:

```xml
<PropertyGroup>
  <!-- Enable diagnostic logging for troubleshooting rewriter issues -->
  <SharpAssertEmitRewriteInfo>true</SharpAssertEmitRewriteInfo>
</PropertyGroup>
```

## Performance

SharpAssert is designed for minimal overhead:

- **Passing tests**: Near-zero overhead - only the assertion check itself
- **Failing tests**: Rich diagnostics are computed only when assertions fail
- **Expression evaluation**: Each sub-expression evaluated exactly once (cached)
- **Build time**: Negligible impact - rewriter processes only test files

The rich diagnostic tools (object diffing, collection comparison) are **only invoked on failure**. 
This means your passing tests run at full speed, and the diagnostic cost is only paid when you need to understand a failure.

## Known Issues

- Collection initializers cannot be used in expression trees (C# compiler limitation)
  - Use `new[]{1,2,3}` instead of `[1, 2, 3]`

## Troubleshooting

### Rewriting not working
1. Verify `SharpAssert` package is installed (SharpAssert.Runtime comes automatically)
2. Ensure `using static SharpAssert.Sharp;` import
3. Clean and rebuild: `dotnet clean && dotnet build`

### No detailed error messages
1. Check build output contains: "SharpAssert: Rewriting X source files"
2. Verify rewritten files exist in `obj/Debug/net9.0/SharpRewritten/`
3. Ensure `SharpInternal.Assert` calls are being made (check generated code)
4. Look for #line directives in generated files

### Enable diagnostic logging
For troubleshooting rewriter issues:
```xml
<PropertyGroup>
  <SharpAssertEmitRewriteInfo>true</SharpAssertEmitRewriteInfo>
</PropertyGroup>
```
Then rebuild with verbose output: `dotnet build -v detailed`

## Contributing

We welcome contributions! Please see our comprehensive [Contributing Guide](CONTRIBUTING.md) for:
- 🚀 Quick start guide for developers
- 🧪 Testing strategy and workflow
- 📦 Package versioning best practices  
- 🔧 Development tips and debugging help
- 📝 Commit guidelines and release process

## License

[MIT License](LICENSE)
