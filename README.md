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

## Features

- **🔍 Detailed Expression Analysis** - See exactly why your assertions failed
- **📦 Simple Setup** - Just add NuGet package, no MSBuild configuration needed
- **🔄 PowerAssert Integration** - Complete support for PowerAssert (switch option)

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

### Custom Error Messages

```csharp
Assert(user.IsActive, $"User {user.Name} should be active for this operation");
```

### Asserting exceptions

```csharp
using static SharpAssert.Sharp;

[Test]
public async Task Throws_catch_exceptions_in_exception_result()
{
    // Thows returns ExceptionResult which allows using them as condition in Assert
    Assert(Throws<ArgumentException>(()=> new ArgumentException("foo")));
    Assert(Throws<ArgumentException>(()=> new ArgumentNullException("bar"))); // will throw unexpected exception
    Assert(!Throws<ArgumentException>(()=> {})); // negative assertion via C# not syntax 

    Assert(Throws<ArgumentException>(()=> 
        new ArgumentException("baz")).Exception.ArgumentName == "baz"); // assert on any custom exception property

    Assert(Throws<ArgumentException>(()=> 
        new ArgumentException("baz")).Data == "baz"); // shortcut form to assert on exception Data property

    Assert(Throws<ArgumentException>(()=> 
        new ArgumentException("bar")).Message.Contains("bar")); // shortcut form to assert on exception Message
    
    // async version
    Assert(await ThrowsAsync<ArgumentException>(()=> 
        Task.Run(() => throw ArgumentException("async")))); // shortcut form to assert on exception Message
 
}
```

## How It Works

SharpAssert uses **MSBuild source rewriting** to automatically transform your assertion calls at compile time:

1. **You write:** `Assert(x == y)` 
2. **MSBuild rewrites:** `global::SharpAssert.SharpInternal.Assert(() => x == y, "x == y", "file.cs", 42)`
3. **Runtime analysis:** Expression tree provides detailed failure diagnostics when assertions fail

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

## Architecture

SharpAssert is built on modern .NET technologies:

- **MSBuild Source Rewriting** - Compile-time code transformation
- **Roslyn Syntax Analysis** - Advanced C# code parsing and generation  
- **Expression Trees** - Runtime expression analysis
- **CallerArgumentExpression** - Fallback for edge cases

### PowerAssert Integration

SharpAssert includes PowerAssert integration and also uses it as a fallback mechanism for not yet implemented features. 

To force PowerAssert for all assertions:

```xml
<PropertyGroup>
  <UsePowerAssert>true</UsePowerAssert>
</PropertyGroup>
```

## Known issues

- Warning about legacy RID used by PowerAssert (dependency). Fix by adding to project properties:
```xml
  <!-- Suppress NETSDK1206 warning from PowerAssert's Libuv dependency -->
  <NoWarn>$(NoWarn);NETSDK1206</NoWarn>
```
- Collection initializers could not be used in expression trees. Compiler limitation. Use `new[]{1,2,3}` instead of `[1, 2, 3]`


## Troubleshooting

### Rewriting not working
1. Verify `SharpAssert` package is installed (SharpAssert.Runtime comes automatically)
2. Ensure `using static SharpAssert.Sharp;` import

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
