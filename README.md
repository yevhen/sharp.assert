<p align="center">
  <img src="https://raw.githubusercontent.com/yevhen/sharp.assert/refs/heads/main/logo.png" alt="SharpAssert logo"/>
</p>

# SharpAssert

A pytest inspired assertion library for .NET with no special syntax.

## Overview

SharpAssert provides rich assertion diagnostics by automatically transforming your assertion expressions at compile time using MSBuild source rewriting, giving you detailed failure messages with powerful expression analysis.

```csharp
using static Sharp;

var items = new[] { 1, 2, 3 };
var target = 4;

Assert(items.Contains(target));
// Assertion failed: items.Contains(target) at MyTest.cs:15
// items:  [1, 2, 3]  
// target: 4
// Result: false
```

## Features

- **🔍 Detailed Expression Analysis** - See exactly why your assertions failed
- **⚡ Zero Runtime Overhead** - No reflection, no performance penalty
- **📦 Simple Setup** - Just add NuGet package, no MSBuild configuration needed
- **🔄 PowerAssert Integration** - Complete support for PowerAssert (switch option)

## Quick Start

### 1. Install Package

```bash
dotnet add package SharpAssert
```

### 2. Use SharpAssert in Your Tests

```csharp
using static Sharp;

[Test]
public void Should_find_matching_item()
{
    var users = new[] { "Alice", "Bob", "Charlie" };
    var searchName = "David";
    
    Assert(users.Contains(searchName));
    // Assertion failed: users.Contains(searchName)
    // users: ["Alice", "Bob", "Charlie"]
    // searchName: "David"  
    // Result: false
}
```

## How It Works

SharpAssert uses **MSBuild source rewriting** to automatically transform your assertion calls at compile time:

1. **You write:** `Assert(x == y)` 
2. **MSBuild rewrites:** `global::SharpAssert.SharpInternal.Assert(() => x == y, "x == y", "file.cs", 42)`
3. **Runtime analysis:** Expression tree provides detailed failure diagnostics when assertions fail
4. **Dual-world design:** Original code preserved for IDE, rewritten code used for compilation

## Advanced Usage

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

### Custom Error Messages

```csharp
Assert(user.IsActive, $"User {user.Name} should be active for this operation");
```

### Async-Safe Usage

```csharp
// Works with async expressions
Assert(await GetBoolAsync()); // Rewritten to provide detailed analysis
```

## Architecture

SharpAssert is built on modern .NET technologies:

- **MSBuild Source Rewriting** - Compile-time code transformation
- **Roslyn Syntax Analysis** - Advanced C# code parsing and generation  
- **Expression Trees** - Runtime expression analysis
- **CallerArgumentExpression** - Fallback for edge cases
- **PowerAssert Backend** - Automatic fallback for complex scenarios

### PowerAssert Integration

SharpAssert includes PowerAssert as an intelligent fallback mechanism. 
When SharpAssert encounters expressions it doesn't yet fully support, it automatically delegates to PowerAssert to ensure you always get meaningful diagnostics. 
This happens transparently - you'll still get detailed error messages regardless of the underlying engine.

**Note:** Async/await and dynamic expressions currently use basic diagnostics via `CallerArgumentExpression`. 
Full support for these features is planned for future releases.

To force PowerAssert for all assertions (useful for comparison or debugging):

```xml
<PropertyGroup>
  <UsePowerAssert>true</UsePowerAssert>
</PropertyGroup>
```

## Troubleshooting

### Rewriter not working
1. Verify `SharpAssert` package is installed (SharpAssert.Runtime comes automatically)
2. Ensure `using static Sharp;` import

### No detailed error messages
1. Check build output contains: "SharpAssert: Rewriting X source files"
2. Verify rewritten files exist in `obj/Debug/net9.0/SharpRewritten/`
3. Ensure `SharpInternal.Assert` calls are being made (check generated code)
4. Look for #line directives in generated files

## Contributing

We welcome contributions! Please see our comprehensive [Contributing Guide](CONTRIBUTING.md) for:
- 🚀 Quick start guide for developers
- 🧪 Testing strategy and workflow
- 📦 Package versioning best practices  
- 🔧 Development tips and debugging help
- 📝 Commit guidelines and release process

## License

[MIT License](LICENSE)
