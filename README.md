# SharpAssert

A pytest-style assertions library for .NET with detailed error reporting and full IDE support.

## Overview

SharpAssert provides rich assertion diagnostics by automatically transforming your assertion expressions at compile time, giving you detailed failure messages without sacrificing performance or IDE experience.

```csharp
using static Sharp;

var items = new[] { 1, 2, 3 };
var target = 4;

Assert(items.Contains(target));
// Assertion failed: items.Contains(target)
// Left:  [1, 2, 3]  
// Right: 4
// Result: false
```

## Features

- **🔍 Detailed Expression Analysis** - See exactly why your assertions failed
- **🚀 Full IDE Support** - IntelliSense, Go to Definition, refactoring all work perfectly  
- **⚡ Zero Runtime Overhead** - No reflection, no performance penalty
- **🛠 Modern C# 12 Interceptors** - Uses official .NET interceptor technology
- **📦 Simple Setup** - Just add NuGet package, no MSBuild configuration needed

## Requirements

- **.NET 8.0 or later** - Required for C# 12 interceptors support
- **C# 12.0 or later** - Uses interceptor language feature  
- **Compatible IDEs** - Visual Studio 2022 17.7+, Rider 2023.3+, VS Code with C# extension

## Quick Start

### 1. Install Package

```bash
dotnet add package SharpAssert
```

### 2. Enable Interceptors in Your Project

Add these properties to your `.csproj` file:

```xml
<PropertyGroup>
  <LangVersion>12.0</LangVersion>
  <Features>InterceptorsPreview</Features>
  <InterceptorsPreviewNamespaces>
    $(InterceptorsPreviewNamespaces);SharpAssert.Generated
  </InterceptorsPreviewNamespaces>
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
    // Array: ["Alice", "Bob", "Charlie"]
    // Search: "David"  
    // Result: false
}
```

## How It Works

SharpAssert uses **C# 12 interceptors** to automatically transform your assertion calls at compile time:

1. **You write:** `Assert(x == y)` 
2. **Compiler generates:** Interceptor that calls `SharpInternal.Assert(() => x == y, "x == y", "file.cs", 42)`
3. **Runtime analysis:** Expression tree provides detailed failure diagnostics
4. **Full IDE support:** Original code remains unchanged, IntelliSense works perfectly

## Benefits Over Traditional Approaches

| Feature | SharpAssert | Traditional Assert |
|---------|-------------|-------------------|
| **IDE Support** | ✅ Full IntelliSense | ❌ Broken by source rewriting |
| **Go to Definition** | ✅ Works perfectly | ❌ Navigates to generated code |
| **Refactoring** | ✅ All tools work | ❌ Breaks on generated files |
| **Setup Complexity** | ✅ Just add NuGet package | ❌ Requires MSBuild configuration |
| **Build Performance** | ✅ Fast compile-time generation | ❌ Slower file I/O processing |
| **Debugging** | ✅ Natural debugging experience | ❌ Debugger confusion |

## Advanced Usage

### Complex Expression Analysis

```csharp
var order = new Order { Items = new[] { "Coffee", "Tea" }, Total = 15.50m };
var expectedTotal = 12.00m;

Assert(order.Items.Length > 0 && order.Total == expectedTotal);
// Assertion failed: order.Items.Length > 0 && order.Total == expectedTotal
// Left:  order.Items.Length > 0 → True (Length: 2)
// Right: order.Total == expectedTotal → False
//   Left:  15.50
//   Right: 12.00
// Result: False
```

### Custom Error Messages

```csharp
Assert(user.IsActive, $"User {user.Name} should be active for this operation");
```

### Async-Safe Usage

```csharp
// Automatically skipped - no interceptor generated for await expressions
Assert(await GetBoolAsync()); // Falls back to CallerArgumentExpression
```

## Architecture

SharpAssert is built on modern .NET technologies:

- **C# 12 Interceptors** - Official compile-time interception
- **Source Generators** - Roslyn-powered code generation  
- **Expression Trees** - Runtime expression analysis
- **CallerArgumentExpression** - Fallback for edge cases

## Clean Install Test

To verify SharpAssert works correctly from a fresh installation:

```bash
# Create new test project
mkdir SharpAssertTest && cd SharpAssertTest
dotnet new console

# Install SharpAssert package
dotnet add package SharpAssert

# Enable interceptors in project file
cat >> SharpAssertTest.csproj << 'EOF'
  <PropertyGroup>
    <LangVersion>12.0</LangVersion>
    <Features>InterceptorsPreview</Features>
    <InterceptorsPreviewNamespaces>
      $(InterceptorsPreviewNamespaces);SharpAssert.Generated
    </InterceptorsPreviewNamespaces>
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

### Test Project Architecture

SharpAssert includes two complementary test projects:

| Project | Reference Type | Purpose | Generator Updates |
|---------|---------------|---------|-------------------|
| **`SharpAssert.IntegrationTest`** | Direct Project | Development/Fast iteration | ✅ Immediate |
| **`SharpAssert.PackageTest`** | NuGet Package | Package validation | ❌ Requires rebuild |

### Integration Test Project (Fast Development)

The `SharpAssert.IntegrationTest` project uses **direct project references** for fast development:

```xml
<!-- Integration test picks up generator changes immediately -->
<ProjectReference Include="../SharpAssert/SharpAssert.csproj" />
<ProjectReference Include="../SharpAssert.Generators/SharpAssert.Generators.csproj" 
                  OutputItemType="Analyzer" 
                  ReferenceOutputAssembly="false" />
```

Benefits:
- ✅ **Generator changes** picked up immediately during `dotnet build`
- ✅ **No package rebuild** needed for testing modifications  
- ✅ **Fast iteration** for development and debugging

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
5. ✅ Verifies interceptors work via NuGet package

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
  ✓ Should_provide_detailed_error_messages_via_interceptors  
✅ All package tests passed!
🎉 The SharpAssert package (v1.0.0-local) works correctly with interceptors.
```

This serves as the **automated Clean Install Test** to ensure packaging works correctly.

## Troubleshooting

### CS9270: InterceptsLocation deprecated warning
This warning appears in .NET 9+ but doesn't affect functionality. Future versions will use `InterceptableLocation`.

### Interceptors not working
1. Verify .NET 8+ and C# 12+
2. Check `Features` and `InterceptorsPreviewNamespaces` in `.csproj`
3. Ensure `using static Sharp;` import
4. Run the Clean Install Test above to verify setup

### No detailed error messages
1. Check that interceptors are enabled
2. Verify build output contains generated interceptor files in `obj/GeneratedFiles`
3. Ensure `SharpInternal.Assert` calls are being made (check generated code)

### Generator changes not reflected
- **Integration Test**: Changes picked up automatically (uses direct references)
- **Package consumers**: Must rebuild package with `dotnet pack` and update version

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