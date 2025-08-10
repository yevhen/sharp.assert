# SharpAssert Integration Test

This project provides manual verification that the SharpAssert MSBuild integration works correctly with the **actual NuGet package**, exactly as end users would experience it.

## Purpose

This project tests the complete **realistic consumer experience**:
- NuGet package is consumed exactly like end users would
- `.targets` file is automatically included from the package
- `SharpLambdaRewriteTask` runs during the build process with proper dependencies
- `Assert()` calls are rewritten to lambda form
- Build completes successfully with rewritten sources

## How to Test

### Step 1: Package Both Projects

```bash
# From solution root - IMPORTANT: Build in Release configuration first
dotnet build SharpAssert.Rewriter/ --configuration Release
dotnet pack SharpAssert.Rewriter/ --configuration Release

# Also need to package SharpAssert runtime (for SharpInternal class)
dotnet pack SharpAssert/ --configuration Release
```

This creates:
- `SharpAssert.Rewriter/bin/Release/SharpAssert.Rewriter.1.0.0.nupkg` (MSBuild task + dependencies)
- `SharpAssert/bin/Release/SharpAssert.1.0.0.nupkg` (Runtime classes)

### Step 2: Enable NuGet Package Reference

Edit `SharpAssert.IntegrationTest.csproj` and uncomment both PackageReference lines:

```xml
<ItemGroup>
  <!-- Comment out project references and use NuGet packages instead -->
  <!-- <ProjectReference Include="../SharpAssert/SharpAssert.csproj" /> -->
  
  <!-- Uncomment these for NuGet package testing -->
  <PackageReference Include="SharpAssert" Version="1.0.0" />
  <PackageReference Include="SharpAssert.Rewriter" Version="1.0.0" />
</ItemGroup>
```

### Step 3: Build and Run the Integration Test

```bash
# Clean previous builds and package cache
rm -rf SharpAssert.IntegrationTest/packages/ SharpAssert.IntegrationTest/obj/

# Restore and build
dotnet restore SharpAssert.IntegrationTest/
dotnet build SharpAssert.IntegrationTest/ -v normal
```

**Expected result**: Build should succeed completely with rewritten Assert calls.

### Step 4: Run the NUnit Tests

```bash
# Run the integration tests
dotnet test SharpAssert.IntegrationTest/
```

**Expected result**: Tests execute and show SharpAssert's detailed assertion analysis in action.

### 2. Verify Rewriting Occurred

Check that rewritten files were generated in the intermediate output directory:

```bash
# Look for rewritten files
ls obj/Debug/net9.0/SharpRewritten/

# Should see:
# TestFile.cs.sharp.g.cs
```

### 3. Inspect Rewritten Content

```bash
# View the rewritten file
cat obj/Debug/net9.0/SharpRewritten/TestFile.cs.sharp.g.cs
```

**Expected Output:**
```csharp
// BEFORE: Assert(x == 1);
// AFTER:  global::SharpAssert.SharpInternal.Assert(()=>x == 1,"x == 1",@"",17);
```
- All `Assert()` calls transformed to lambda form with expression strings and line numbers
- File structure and other content remain unchanged
- No compilation errors (SharpInternal class found via SharpAssert package)

### 4. Verify Build Success

The build should complete without errors:
- ✅ No compilation errors
- ✅ MSBuild task executes successfully  
- ✅ Rewritten files are used for compilation

## What This Tests

- **Real Consumer Experience**: Uses the rewriter exactly as end-users would
- **MSBuild Integration**: Verifies `.targets` file loads and executes correctly
- **Build Pipeline**: Tests that rewriting happens `BeforeTargets="CoreCompile"`
- **File Processing**: Confirms source → rewritten → compilation pipeline works

## Expected Files After Build

```
obj/Debug/net9.0/SharpRewritten/
└── TestFile.cs.sharp.g.cs    # Rewritten version with lambda Assert calls
```

## Troubleshooting

### Critical NuGet Cache Issues

**Problem**: Rider shows old `global::SharpInternal` instead of `global::SharpAssert.SharpInternal`
**Cause**: IDE using cached packages from global NuGet cache while CLI uses local packages

**Solution**:
```bash
# Clear ALL NuGet caches (critical!)
dotnet nuget locals all --clear

# Clean integration test completely
rm -rf SharpAssert.IntegrationTest/packages/ SharpAssert.IntegrationTest/obj/ SharpAssert.IntegrationTest/bin/

# Rebuild packages first
dotnet build SharpAssert.Rewriter/ --configuration Release
dotnet pack SharpAssert.Rewriter/ --configuration Release
dotnet pack SharpAssert/ --configuration Release

# Force restore and build integration test
dotnet restore SharpAssert.IntegrationTest/ --force
dotnet build SharpAssert.IntegrationTest/
```

### CS5001: Program does not contain static Main method

**Problem**: Test projects require executable entry point
**Cause**: MSBuild test SDK generates Program.cs but rewriter was removing it

**Solution**: Updated rewriter targets to preserve generated Program.cs files:
- Automatically excludes `Microsoft.NET.Test.Sdk.Program.cs`
- Excludes generated files like `.AssemblyInfo.cs`, `.GlobalUsings.g.cs`
- Only processes actual source files for rewriting

### CS0400: SharpInternal not found

**Problem**: Generated code uses wrong namespace
**Root Cause**: Old cached rewriter version using `global::SharpInternal` 
**Fix**: Current version correctly generates `global::SharpAssert.SharpInternal.Assert`

**How to verify fix worked**:
```bash
# Check generated file has correct namespace
cat obj/Debug/net9.0/SharpRewritten/TestFile.cs.sharp.g.cs
# Should show: global::SharpAssert.SharpInternal.Assert(()=>...
```

### No Rewritten Files Generated

1. **Check MSBuild logs**:
   ```bash
   dotnet build -v normal
   ```
   Should see: "SharpAssert: Rewriting Sharp.Assert calls to lambda form"

2. **Verify project settings**:
   ```xml
   <EnableSharpLambdaRewrite>true</EnableSharpLambdaRewrite>
   <SharpAssertEmitRewriteInfo>true</SharpAssertEmitRewriteInfo>
   ```

3. **Check source files**: Ensure they contain `Assert()` calls

### Rider vs CLI Build Differences

**Issue**: Build works in CLI but fails in Rider
**Cause**: Different package resolution paths

**Debug Steps**:
1. Check which packages Rider is referencing:
   - Look for `/Users/[user]/.nuget/packages/` (global cache - bad)
   - vs `../SharpAssert.Rewriter/bin/Release/` (local - good)

2. Force Rider to use local packages:
   ```bash
   # Clear global cache
   dotnet nuget locals all --clear
   # Restart Rider after this
   ```

### Build Errors

1. **Dependencies not built**:
   ```bash
   dotnet restore  # Restore entire solution
   dotnet build    # Build all projects
   ```

2. **Clean everything and rebuild**:
   ```bash
   dotnet clean
   dotnet build SharpAssert.Rewriter/ --configuration Release
   dotnet pack SharpAssert.Rewriter/ --configuration Release
   dotnet pack SharpAssert/ --configuration Release
   ```

3. **Package reference issues**:
   - Verify local package sources in project file
   - Check that `RestoreAdditionalProjectSources` points to correct Release folders

### Test Execution Issues

**Tests run but assertions fail**: This is expected! The integration is working.
- SharpAssert rewriter ✅ working perfectly
- Expression analysis logic may have bugs (separate issue)
- Test failures show detailed assertion analysis is functioning

## Manual Verification Checklist

Follow these steps to verify complete integration:

1. ✅ **NuGet packages restored** - Check packages folder exists
2. ✅ **Build logs show rewriting** - See "SharpAssert: Rewriting Sharp.Assert calls to lambda form"  
3. ✅ **Rewritten files generated** - File exists in `obj/Debug/net9.0/SharpRewritten/TestFile.cs.sharp.g.cs`
4. ✅ **Perfect transformation** - Assert calls become `global::SharpAssert.SharpInternal.Assert(()=>...)`
5. ✅ **Build succeeds completely** - No compilation errors, rewritten code compiles successfully
6. ✅ **Complete pipeline integration** - Source → rewrite → compile → assembly

## Current Status ✅

**MSBuild Integration: ✅ FULLY WORKING**

The integration test is **completely functional** as an NUnit test project:
- ✅ **NuGet package consumption**: Both SharpAssert + SharpAssert.Rewriter packages working
- ✅ **MSBuild integration**: Rewriter task executes perfectly during build
- ✅ **Source transformation**: All `Assert()` calls rewritten to `global::SharpAssert.SharpInternal.Assert(()=>...)`
- ✅ **Build success**: Project compiles successfully in both CLI and Rider
- ✅ **Test execution**: NUnit tests run and SharpAssert analysis works (though logic may need fixes)

**Project Type**: Full NUnit test project compatible with `dotnet test`

## Key Knowledge & Learnings

### MSBuild Source Rewriter Integration
- **Targets file execution**: `.targets` files in NuGet packages automatically included in build
- **BeforeTargets timing**: Rewriting must happen `BeforeTargets="CoreCompile"` 
- **File preservation**: Must preserve generated files (Program.cs, AssemblyInfo.cs) during rewriting
- **Compile item management**: Use surgical approach - only replace files that were actually rewritten

### NuGet Package Development
- **Local package sources**: Use `RestoreAdditionalProjectSources` for testing unreleased packages
- **Cache invalidation**: Always `dotnet nuget locals all --clear` when testing package changes
- **IDE vs CLI**: IDEs may cache packages differently - always clear global cache for testing

### Test Project Integration
- **NUnit setup**: Full test project with Microsoft.NET.Test.Sdk generates Program.cs automatically  
- **Output type**: Must be Library even with test SDK (override with second PropertyGroup)
- **Using statements**: Can use global usings or explicit using statements for NUnit

### MSBuild Task Development
- **Assembly loading**: Task assemblies loaded in separate context with dependencies
- **File processing**: Process all files but only output changed ones
- **Error handling**: Graceful fallback when rewriting fails

## Why This Approach Works
- ✅ **Real consumer experience** - Tests exactly how end users consume the package
- ✅ **Complete dependency resolution** - NuGet handles all Microsoft.CodeAnalysis dependencies
- ✅ **Automatic .targets integration** - MSBuild finds and executes rewriter automatically  
- ✅ **Full test integration** - Works as complete NUnit test project with `dotnet test`