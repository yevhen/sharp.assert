<p align="center">
  <img src="https://github.com/yevhen/sharp.assert/blob/master/Logo.png?raw=true" alt="SharpAssert logo"/>
</p>

# SharpAssert

A pytest inspired assertion library for .NET that provides detailed error reporting with no special syntax.

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
- **🚀 Full IDE Support** - IntelliSense, Go to Definition, refactoring all work perfectly  
- **⚡ Zero Runtime Overhead** - No reflection, no performance penalty
- **🛠 MSBuild Source Rewriting** - Compile-time transformation with #line directives for debugging
- **📦 Simple Setup** - Just add NuGet package, no MSBuild configuration needed
- **🔄 Automatic Fallback** - PowerAssert integration ensures all assertions work, even for features still in development

## Requirements

- **.NET 9.0 or later** - Required for MSBuild source rewriting support
- **C# 13.0 or later** - Modern language features for expression analysis  
- **Compatible IDEs** - Visual Studio 2022 17.7+, Rider 2023.3+, VS Code with C# extension

## Quick Start

### 1. Install Package

```bash
dotnet add package SharpAssert
```

### 2. Update Your Project File

Add modern C# language version to your `.csproj` file:

```xml
<PropertyGroup>
  <LangVersion>13.0</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

### 3. Use SharpAssert in Your Tests

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

## Benefits Over Traditional Approaches

| Feature               | SharpAssert                    | Traditional Assert               |
|-----------------------|--------------------------------|----------------------------------|
| **IDE Support**       | ✅ Full IntelliSense            | ❌ Broken by source rewriting     |
| **Go to Definition**  | ✅ Works perfectly              | ❌ Navigates to generated code    |
| **Refactoring**       | ✅ All tools work               | ❌ Breaks on generated files      |
| **Setup Complexity**  | ✅ Just add NuGet package       | ❌ Requires MSBuild configuration |
| **Build Performance** | ✅ Fast compile-time generation | ❌ Slower file I/O processing     |
| **Debugging**         | ✅ Natural debugging experience | ❌ Debugger confusion             |

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

## Clean Install Test

To verify SharpAssert works correctly from a fresh installation:

```bash
# Create new test project
mkdir SharpAssertTest && cd SharpAssertTest
dotnet new console

# Install SharpAssert package
dotnet add package SharpAssert

# Install both packages
dotnet add package SharpAssert.Rewriter

# Update project file with modern C#
cat >> SharpAssertTest.csproj << 'EOF'
  <PropertyGroup>
    <LangVersion>13.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
EOF

# Create test code
cat > Program.cs << 'EOF'
using static Sharp;

var x = 5;
var y = 10;
var items = new[] { 1, 2, 3 };

Console.WriteLine("Testing SharpAssert...");

// This should pass
Assert(x < y);
Console.WriteLine("✓ Simple assertion passed");

// This should fail with detailed message
try 
{
    Assert(items.Contains(999));
}
catch (SharpAssertionException ex)
{
    Console.WriteLine($"✓ Detailed error message: {ex.Message}");
}

Console.WriteLine("SharpAssert is working correctly!");
EOF

# Build and run
dotnet build
dotnet run
```

**Expected output:**
```
Testing SharpAssert...
✓ Simple assertion passed
✓ Detailed error message: Assertion failed: items.Contains(999)
  Array: [1, 2, 3]
  Search: 999
  Result: false
SharpAssert is working correctly!
```

## Development Integration

### Development Workflow

SharpAssert uses a **Local NuGet Feed** approach for development and testing:

```bash
# Publish to local feed and test in one command
./test-local.sh

# Or run steps manually:
./publish-local.sh          # Publish packages to local-feed/
dotnet test SharpAssert.PackageTest/  # Test with local packages
```

### Local Development Benefits

- ✅ **Simple workflow** - Single command testing
- ✅ **No cache management** - Timestamp-based versioning  
- ✅ **No file editing** - Stable wildcard package references
- ✅ **Professional approach** - Standard NuGet development pattern

### Package Test Project (Package Validation)

The `SharpAssert.PackageTest` project verifies the **actual NuGet package** works correctly:

```bash
# Run the automated package test
cd SharpAssert.PackageTest
./test-local-package.sh
```

The script automatically:
1. 🧹 Cleans packages directory to avoid version conflicts
2. 📦 Builds and packs SharpAssert with `-local` suffix  
3. 🔧 Updates test project to use the exact package version
4. 🧪 Runs comprehensive package validation tests
5. ✅ Confirms Assert calls are properly transformed

**Example output:**
```
🔧 SharpAssert Local Package Test
================================================
🧹 Cleaning packages directory...
📦 Building SharpAssert with local suffix...
✅ Built package: SharpAssert.1.0.0-local
📋 Package version: 1.0.0-local
🧪 Running package tests...
  ✓ Should_support_basic_assertions_via_package
  ✓ Should_provide_detailed_error_messages  
✅ All package tests passed!
🎉 SharpAssert packages work correctly from local feed
```

This serves as the **automated Clean Install Test** to ensure packaging works correctly.

## Troubleshooting

### CS9270: InterceptsLocation deprecated warning
This warning appears in .NET 9+ but doesn't affect functionality. Future versions will use `InterceptableLocation`.

### Rewriter not working
1. Verify both `SharpAssert` and `SharpAssert.Rewriter` packages are installed
2. Check .NET 9+ and C# 13+ are configured
3. Ensure `using static Sharp;` import
4. Run `./test-local.sh` to verify complete setup

### No detailed error messages
1. Check build output contains: "SharpAssert: Rewriting X source files"
2. Verify rewritten files exist in `obj/Debug/net9.0/SharpRewritten/`
3. Ensure `SharpInternal.Assert` calls are being made (check generated code)
4. Look for #line directives in generated files

### Development workflow
- Use `./publish-local.sh` to update local packages
- Run `./test-local.sh` for comprehensive validation  
- Local feed uses timestamp versioning to avoid cache issues

## Contributing

We welcome contributions! Please see our comprehensive [Contributing Guide](CONTRIBUTING.md) for:
- 🚀 Quick start guide for developers
- 🧪 Testing strategy and workflow
- 📦 Package versioning best practices  
- 🔧 Development tips and debugging help
- 📝 Commit guidelines and release process

## License

[MIT License](LICENSE)

---

**SharpAssert** - Rich assertion diagnostics with full IDE support 🚀
