# MSBuild Integration Tests Plan for SharpLambdaRewriteTask

## Overview
Create simple MSBuild integration tests using a **static test project** that exercises the MSBuild task exactly as consumers would experience it. **No mocks or stubs** - use real MSBuild execution and the actual .targets file integration.

## Key Insight
The heavy lifting (source code rewriting logic) is already comprehensively tested in `SharpAssertRewriter.cs` unit tests. The MSBuild task primarily handles file I/O plumbing:
- Read source files from disk
- Call `SharpAssertRewriter.Rewrite()`  
- Write results to output directory
- Handle basic error scenarios

## Static Test Project Approach

### Project Structure
Create a dedicated integration test project within the solution:

```
SharpAssert.IntegrationTests/
├── SharpAssert.IntegrationTests.csproj  # Enables rewriter via .targets reference
├── TestSources/
│   ├── SimpleAsserts.cs                 # Basic Assert(x == y) calls  
│   ├── NoAsserts.cs                     # Files without Assert calls
│   ├── ComplexExpressions.cs            # LINQ, method calls in asserts
│   ├── AsyncAsserts.cs                  # Contains await (should skip rewrite)
│   └── InvalidSyntax.cs                 # Malformed code for fallback testing
└── BuildIntegrationFixture.cs           # Tests that trigger actual builds
```

### Test Project Configuration (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <EnableSharpLambdaRewrite>true</EnableSharpLambdaRewrite>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="../SharpAssert/SharpAssert.csproj" />
    <ProjectReference Include="../SharpAssert.Rewriter/SharpAssert.Rewriter.csproj" />
  </ItemGroup>
</Project>
```

## Test Implementation

### Core Integration Test
```csharp
[TestFixture]
public class BuildIntegrationFixture  
{
    [Test]
    public void Should_rewrite_assert_calls_during_build()
    {
        // Trigger real MSBuild on this project itself
        var buildResult = ExecuteMSBuild("Build");
        
        buildResult.Should().BeSuccessful();
        
        // Verify rewritten files exist in intermediate output
        var rewrittenDir = Path.Combine("obj", "Debug", "net9.0", "SharpRewritten");
        
        File.Exists(Path.Combine(rewrittenDir, "TestSources", "SimpleAsserts.cs.sharp.g.cs"))
            .Should().BeTrue();
            
        // Verify content was rewritten correctly
        var rewrittenContent = File.ReadAllText(
            Path.Combine(rewrittenDir, "TestSources", "SimpleAsserts.cs.sharp.g.cs"));
        rewrittenContent.Should().Contain("SharpInternal.Assert(() =>");
        rewrittenContent.Should().NotContain("Assert(");
    }
    
    [Test]
    public void Should_copy_files_without_asserts_unchanged()
    {
        var buildResult = ExecuteMSBuild("Build");
        buildResult.Should().BeSuccessful();
        
        var originalContent = File.ReadAllText("TestSources/NoAsserts.cs");
        var rewrittenContent = File.ReadAllText(
            Path.Combine("obj", "Debug", "net9.0", "SharpRewritten", 
                        "TestSources", "NoAsserts.cs.sharp.g.cs"));
        
        rewrittenContent.Should().Be(originalContent);
    }
    
    [Test] 
    public void Should_handle_async_asserts_gracefully()
    {
        var buildResult = ExecuteMSBuild("Build");
        buildResult.Should().BeSuccessful();
        
        var rewrittenContent = File.ReadAllText(
            Path.Combine("obj", "Debug", "net9.0", "SharpRewritten",
                        "TestSources", "AsyncAsserts.cs.sharp.g.cs"));
                        
        // Should preserve original Assert calls when await is present
        rewrittenContent.Should().Contain("Assert(await");
        rewrittenContent.Should().NotContain("SharpInternal.Assert(() => await");
    }
    
    [Test]
    public void Should_fallback_gracefully_on_invalid_syntax()
    {
        var buildResult = ExecuteMSBuild("Build");
        
        // Build should succeed even with invalid files
        buildResult.Should().BeSuccessful();
        
        // Invalid files should be copied as-is (fallback behavior)  
        var fallbackFile = Path.Combine("obj", "Debug", "net9.0", "SharpRewritten",
                                       "TestSources", "InvalidSyntax.cs.sharp.g.cs");
        File.Exists(fallbackFile).Should().BeTrue();
    }
}
```

## Test Source Files

### TestSources/SimpleAsserts.cs
```csharp
using static Sharp;

public class SimpleAssertTests
{
    public void TestMethod()
    {
        var x = 1;
        var y = 2;
        Assert(x == y);
        Assert(x < y);
        Assert(items.Contains(item));
    }
}
```

### TestSources/AsyncAsserts.cs  
```csharp
using static Sharp;

public class AsyncTests
{
    public async Task TestAsync()
    {
        Assert(await GetBoolAsync());
        Assert(await GetValueAsync() == 42);
    }
}
```

## MSBuild Execution Helper

```csharp
public static class MSBuildHelper
{
    public static BuildResult ExecuteMSBuild(string target)
    {
        var projectPath = Path.GetFullPath("SharpAssert.IntegrationTests.csproj");
        
        using var projectCollection = new ProjectCollection();
        var project = projectCollection.LoadProject(projectPath);
        
        return project.Build(target) ? BuildResult.Success : BuildResult.Failure;
    }
}
```

## What This Tests

✅ **Real MSBuild Integration** - Uses actual .targets file exactly like consumers  
✅ **File Processing Pipeline** - Read → rewrite → write workflow  
✅ **Output Directory Structure** - Correct intermediate file placement  
✅ **Error Handling** - Graceful fallback for invalid code  
✅ **Build Success** - Entire build completes without failures  
✅ **Async Detection** - Files with await skip rewriting correctly

## Benefits of This Approach

- **Simple Setup** - Static project files, no dynamic generation complexity
- **Fast Execution** - Single build per test, not orchestration overhead  
- **Realistic Testing** - Exact consumer experience using real .targets integration
- **Easy Maintenance** - Add test scenarios by adding source files
- **Focused Coverage** - Tests the MSBuild plumbing, not the rewriter logic (already tested)

## Implementation Steps

1. **Create Integration Test Project** - Add `SharpAssert.IntegrationTests.csproj` to solution
2. **Add Test Source Files** - Create various Assert patterns in `TestSources/` directory  
3. **Implement Build Tests** - Create `BuildIntegrationFixture.cs` with MSBuild execution
4. **Verify .targets Integration** - Ensure task runs at correct build phase
5. **Test Error Scenarios** - Verify graceful fallback behavior

## Success Criteria

- ✅ Tests use real MSBuild execution (no mocks)
- ✅ .targets file integration verified through actual builds  
- ✅ File generation and content verified with real file system operations
- ✅ Error scenarios tested with actual invalid code
- ✅ Build performance impact is measurable and acceptable

This approach provides comprehensive MSBuild integration testing while remaining simple, maintainable, and realistic.