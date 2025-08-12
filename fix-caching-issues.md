# Fix Caching Issues Implementation Plan

This document provides a comprehensive step-by-step plan to fix the 4 critical NuGet caching and MSBuild issues while preserving the dev package workflow.

## Overview

The current dev workflow uses timestamp-based versioning (`1.0.0-dev20250812015148`) with local feed publishing via `publish-local.sh`. This workflow is **ultra-critical** and must be preserved. The fixes address:

1. **Generated file cleanup** - `dotnet clean` doesn't remove `SharpRewritten/` folder
2. **MSBuild input/output tracking** - Incremental builds miss rewriter assembly changes
3. **Package resolution updates** - Stale `project.assets.json` problem
4. **Task assembly versioning** - Cached task assembly detection

## Prerequisites

- Current working directory: `/Users/yb/work/oss/SharpAssert`
- Git branch: `main` (ahead by 10 commits)
- No uncommitted changes

## Implementation Order

The fixes are ordered by dependency and risk level:
1. Generated file cleanup (lowest risk, foundational)
2. Input/output tracking (medium risk, affects build logic)
3. Task assembly versioning (medium risk, affects MSBuild)
4. Package resolution updates (highest risk, affects dev workflow)

---

## Fix 1: Add Generated File Cleanup Target

**Problem:** `dotnet clean` doesn't remove the `SharpRewritten/` folder, leaving stale generated files.

**Impact:** Stale files can cause confusing build behavior and mask real issues.

### Steps

#### 1.1 Add Cleanup Target to MSBuild

**File:** `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets`

**Change:** Add cleanup target after line 56 (end of file):

```xml
  <!-- Clean target to remove generated files -->
<Target Name="SharpAssertClean" BeforeTargets="Clean">
    <PropertyGroup>
        <SharpRewrittenPath>$(IntermediateOutputPath)SharpRewritten</SharpRewrittenPath>
    </PropertyGroup>

    <Message Text="SharpAssert: Cleaning generated files from $(SharpRewrittenPath)"
             Importance="low"
             Condition="Exists('$(SharpRewrittenPath)')" />

    <RemoveDir Directories="$(SharpRewrittenPath)"
               Condition="Exists('$(SharpRewrittenPath)')" />

    <ItemGroup>
        <FileWrites Include="$(SharpRewrittenPath)/**/*" />
    </ItemGroup>
</Target>
```

#### 1.2 Verification Steps

Run these commands to verify the fix:

```bash
# Test cleanup in a test project
cd SharpAssert.PackageTest
dotnet build
# Verify SharpRewritten folder exists
ls -la obj/Debug/net9.0/SharpRewritten/ 2>/dev/null && echo "✅ Generated files exist" || echo "❌ No generated files found"

# Test clean
dotnet clean
# Verify SharpRewritten folder is removed
ls -la obj/Debug/net9.0/SharpRewritten/ 2>/dev/null && echo "❌ Cleanup failed" || echo "✅ Cleanup successful"
```

#### 1.3 Integration Test

```bash
# Full workflow test
./test-local.sh
# Should pass without errors
```

#### 1.4 Rollback Plan

If cleanup target causes issues:
```bash
git checkout HEAD -- SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets
```

---

## Fix 2: Implement Proper MSBuild Input/Output Tracking

**Problem:** Incremental builds don't detect when rewriter assembly changes, leading to stale generated files.

**Impact:** Changes to rewriter logic don't trigger regeneration of source files.

### Steps

#### 2.1 Add Input Dependencies to Rewrite Target

**File:** `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets`

**Change:** Modify the `SharpLambdaRewrite` target (around line 14):

```xml
  <Target Name="SharpLambdaRewrite" BeforeTargets="CoreCompile"
          Condition="'$(EnableSharpLambdaRewrite)'=='true' and '$(DesignTimeBuild)' != 'true' and '$(BuildingForLiveUnitTesting)' != 'true'"
          Inputs="@(_SharpSourceFiles);$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.dll;$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.deps.json"
          Outputs="@(_SharpSourceFiles->'$(IntermediateOutputPath)SharpRewritten\%(RecursiveDir)%(Filename)%(Extension).sharp.g.cs')">
```

#### 2.2 Add Dependency Items Before Target

**Change:** Insert before `SharpLambdaRewrite` target (around line 14):

```xml
  <!-- Define rewriter dependencies for incremental build tracking -->
<ItemGroup>
    <_SharpRewriterDependencies Include="$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.dll" />
    <_SharpRewriterDependencies Include="$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.deps.json" />
    <_SharpRewriterDependencies Include="$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.runtimeconfig.json"
                                Condition="Exists('$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.runtimeconfig.json')" />
</ItemGroup>
```

#### 2.3 Enhanced Target with Dependencies

**Change:** Replace the entire `SharpLambdaRewrite` target:

```xml
  <Target Name="SharpLambdaRewrite" BeforeTargets="CoreCompile"
          Condition="'$(EnableSharpLambdaRewrite)'=='true' and '$(DesignTimeBuild)' != 'true' and '$(BuildingForLiveUnitTesting)' != 'true'"
          Inputs="@(_SharpSourceFiles);@(_SharpRewriterDependencies)"
          Outputs="@(_SharpSourceFiles->'$(IntermediateOutputPath)SharpRewritten\%(RecursiveDir)%(Filename)%(Extension).sharp.g.cs')">

    <ItemGroup>
        <_SharpInput Include="@(Compile)" />
        <_SharpNonSourceFiles Include="@(Compile)" Condition="$([System.String]::Copy('%(Identity)').Contains('Microsoft.NET.Test.Sdk.Program.cs')) or $([System.String]::Copy('%(Identity)').EndsWith('.AssemblyInfo.cs')) or $([System.String]::Copy('%(Identity)').EndsWith('.AssemblyAttributes.cs')) or $([System.String]::Copy('%(Identity)').EndsWith('.GlobalUsings.g.cs'))" />
        <_SharpSourceFiles Include="@(Compile)" Exclude="@(_SharpNonSourceFiles)" />
    </ItemGroup>

    <Message Text="SharpAssert: Rewriting Sharp.Assert calls to lambda form" Importance="normal"
             Condition="'$(SharpAssertEmitRewriteInfo)'=='true'" />

    <Message Text="SharpAssert: Incremental build triggered - rewriter dependencies changed"
             Importance="normal"
             Condition="'$(SharpAssertEmitRewriteInfo)'=='true'" />

    <SharpLambdaRewriteTask
            Sources="@(_SharpInput)"
            ProjectDir="$(MSBuildProjectDirectory)"
            IntermediateDir="$(IntermediateOutputPath)"
            OutputDir="$(IntermediateOutputPath)SharpRewritten"
            LangVersion="$(LangVersion)"
            UsePowerAssert="$(SharpAssertUsePowerAssert)"
            UsePowerAssertForUnsupported="$(SharpAssertUsePowerAssertForUnsupported)" />

    <ItemGroup>
        <!-- Track rewritten files for potential cleanup -->
        <_SharpRewrittenFiles Include="$(IntermediateOutputPath)SharpRewritten\**\*.sharp.g.cs" />
        <!-- Only during actual build: swap original files with rewritten versions -->
        <Compile Remove="@(_SharpSourceFiles)" />
        <Compile Include="@(_SharpRewrittenFiles)" />

        <!-- Add generated files to clean tracking -->
        <FileWrites Include="@(_SharpRewrittenFiles)" />
    </ItemGroup>

    <Message Text="SharpAssert: Replaced @(_SharpSourceFiles->Count()) source files with rewritten versions (actual build)" Importance="normal"
             Condition="'$(SharpAssertEmitRewriteInfo)'=='true'" />

</Target>
```

#### 2.4 Verification Steps

```bash
# Test incremental build detection
cd SharpAssert.PackageTest

# Build once
dotnet build
echo "First build completed"

# Touch the rewriter assembly to simulate an update
touch ../SharpAssert.Rewriter/bin/Release/net9.0/SharpAssert.Rewriter.dll

# Rebuild - should detect dependency change
dotnet build --verbosity normal 2>&1 | grep -i "sharp"
# Should show "Incremental build triggered" message

# Verify no changes to source - should skip rewrite
dotnet build --verbosity normal 2>&1 | grep -i "sharp" 
# Should show "Target skipped" or similar
```

#### 2.5 Integration Test

```bash
# Test with dev workflow
./publish-local.sh
./test-local.sh
# Should pass without issues
```

#### 2.6 Rollback Plan

```bash
git checkout HEAD -- SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets
```

---

## Fix 3: Add Task Assembly Versioning Checks

**Problem:** MSBuild caches task assemblies and doesn't reload when they change during development.

**Impact:** Changes to `SharpLambdaRewriteTask` aren't reflected until MSBuild process restarts.

### Steps

#### 3.1 Add Assembly Timestamp Tracking to Task

**File:** `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/SharpLambdaRewriteTask.cs`

**Change:** Add assembly change detection after line 33:

```csharp
[Output]
public ITaskItem[] GeneratedFiles { get; set; } = [];

// Add assembly versioning check
public string TaskAssemblyPath { get; set; } = string.Empty;
public string TaskAssemblyLastWriteTime { get; set; } = string.Empty;
```

#### 3.2 Add Version Check Logic

**Change:** Add method before `ExecuteInternal()` (around line 47):

```csharp
bool CheckTaskAssemblyVersion()
{
    try
    {
        var currentAssemblyPath = GetType().Assembly.Location;
        var currentLastWrite = File.GetLastWriteTime(currentAssemblyPath).ToString("O");
        
        if (!string.IsNullOrEmpty(TaskAssemblyLastWriteTime) && 
            TaskAssemblyLastWriteTime != currentLastWrite)
        {
            Log.LogMessage(MessageImportance.Normal, 
                $"SharpAssert: Task assembly updated, forcing full rebuild");
            return false;
        }
        
        TaskAssemblyLastWriteTime = currentLastWrite;
        return true;
    }
    catch (Exception ex)
    {
        LogDiagnostics($"Assembly version check failed: {ex.Message}");
        return true; // Continue on errors
    }
}
```

#### 3.3 Integrate Version Check

**Change:** Modify `ExecuteInternal()` method (around line 47):

```csharp
bool ExecuteInternal()
{
    Log.LogMessage(MessageImportance.Normal, $"SharpAssert: Rewriting {Sources.Length} source files");

    // Check if task assembly has been updated
    if (!CheckTaskAssemblyVersion())
    {
        Log.LogMessage(MessageImportance.High, 
            "SharpAssert: Task assembly changed - forcing regeneration of all files");
    }

    LogDiagnostics($"Project directory: {ProjectDir}");
    LogDiagnostics($"Output directory: {OutputDir}");
    LogDiagnostics($"Language version: {LangVersion}");

    EnsureDirectoryExists(OutputDir);
    // ... rest of method unchanged
```

#### 3.4 Update MSBuild Integration

**File:** `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets`

**Change:** Pass assembly info to task (around the `SharpLambdaRewriteTask` call):

```xml
<SharpLambdaRewriteTask
        Sources="@(_SharpInput)"
        ProjectDir="$(MSBuildProjectDirectory)"
        IntermediateDir="$(IntermediateOutputPath)"
        OutputDir="$(IntermediateOutputPath)SharpRewritten"
        LangVersion="$(LangVersion)"
        UsePowerAssert="$(SharpAssertUsePowerAssert)"
        UsePowerAssertForUnsupported="$(SharpAssertUsePowerAssertForUnsupported)"
        TaskAssemblyPath="$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.dll"
        TaskAssemblyLastWriteTime="$([System.IO.File]::GetLastWriteTime('$(MSBuildThisFileDirectory)../tools/net9.0/SharpAssert.Rewriter.dll').ToString('O'))" />
```

#### 3.5 Verification Steps

```bash
# Test assembly change detection
cd SharpAssert.PackageTest

# Build once
dotnet build --verbosity normal 2>&1 | tee build1.log
grep -i "Task assembly" build1.log

# Simulate rewriter update by republishing
cd ..
./publish-local.sh

# Restore and build again
cd SharpAssert.PackageTest  
dotnet restore
dotnet build --verbosity normal 2>&1 | tee build2.log
grep -i "assembly updated\|forcing" build2.log
```

#### 3.6 Integration Test

```bash
# Full dev workflow test
./test-local.sh
# Should detect assembly changes and rebuild appropriately
```

#### 3.7 Rollback Plan

```bash
git checkout HEAD -- SharpAssert.Rewriter/SharpLambdaRewriteTask.cs
git checkout HEAD -- SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets
```

---

## Fix 4: Force Package Resolution Updates in Dev Workflow

**Problem:** Stale `project.assets.json` files cause new dev packages to be ignored.

**Impact:** Changes aren't picked up until manual intervention (delete obj/, dotnet restore --force, etc.)

### Steps

#### 4.1 Add Force Restore to Publish Script

**File:** `/Users/yb/work/oss/SharpAssert/publish-local.sh`

**Change:** Add forced package clearing after line 33 (after packing):

```bash
dotnet pack SharpAssert.Rewriter/SharpAssert.Rewriter.csproj \
    --configuration Release \
    --output local-feed \
    -p:PackageVersion="$VERSION" \
    --verbosity quiet

echo -e "${GREEN}✅ Published packages to local feed:${NC}"
echo -e "  📋 SharpAssert $VERSION"
echo -e "  📋 SharpAssert.Rewriter $VERSION"
echo -e "${BLUE}📁 Feed location: ./local-feed/${NC}"

echo -e "${YELLOW}🔄 Forcing package cache refresh...${NC}"

# Clear package cache for SharpAssert packages
dotnet nuget locals all --clear

# Force update of consuming projects  
for project in SharpAssert.PackageTest SharpAssert.PowerAssertTest; do
    if [ -d "$project" ]; then
        echo -e "${YELLOW}  📦 Updating $project...${NC}"
        # Remove obj folder to clear project.assets.json
        rm -rf "$project/obj"
        # Force restore with no-cache to pick up new packages
        dotnet restore "$project" --no-cache --force-evaluate --verbosity quiet
    fi
done

echo ""
echo -e "${YELLOW}💡 Now run: dotnet build && dotnet test${NC}"
```

#### 4.2 Enhance Test Script for Package Updates

**File:** `/Users/yb/work/oss/SharpAssert/test-local.sh`

**Change:** Update restore section (around line 17-19):

```bash
echo -e "${YELLOW}🔄 Restoring packages from local feed...${NC}"

# Force clean restore to pick up latest packages
echo -e "${YELLOW}  🗑️ Clearing build artifacts...${NC}"
dotnet clean SharpAssert.PackageTest/ --verbosity quiet 2>/dev/null || true
dotnet clean SharpAssert.PowerAssertTest/ --verbosity quiet 2>/dev/null || true

echo -e "${YELLOW}  📦 Force restoring packages...${NC}"
dotnet restore SharpAssert.PackageTest/ --no-cache --force-evaluate --verbosity quiet
dotnet restore SharpAssert.PowerAssertTest/ --no-cache --force-evaluate --verbosity quiet
```

#### 4.3 Add Package Update Verification

**Change:** Add verification after restore (around line 19):

```bash
dotnet restore SharpAssert.PowerAssertTest/ --no-cache --force-evaluate --verbosity quiet

echo -e "${YELLOW}🔍 Verifying package versions...${NC}"

# Check which version of SharpAssert packages were restored
for project in SharpAssert.PackageTest SharpAssert.PowerAssertTest; do
    if [ -f "$project/obj/project.assets.json" ]; then
        SHARP_VERSION=$(grep -o '"SharpAssert/[^"]*"' "$project/obj/project.assets.json" | head -1 | cut -d'/' -f2 | tr -d '"')
        REWRITER_VERSION=$(grep -o '"SharpAssert.Rewriter/[^"]*"' "$project/obj/project.assets.json" | head -1 | cut -d'/' -f2 | tr -d '"')
        echo -e "  📋 $project: SharpAssert $SHARP_VERSION, Rewriter $REWRITER_VERSION"
    fi
done
echo ""
```

#### 4.4 Add Development Helper Script

**File:** `/Users/yb/work/oss/SharpAssert/force-refresh.sh`

Create new script for forcing package refresh:

```bash
#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔄 Force Package Refresh${NC}"
echo "======================="

echo -e "${YELLOW}🗑️ Clearing all caches...${NC}"

# Clear NuGet caches
dotnet nuget locals all --clear

# Clear build artifacts from all projects
for project in SharpAssert SharpAssert.Rewriter SharpAssert.Tests SharpAssert.Rewriter.Tests SharpAssert.PackageTest SharpAssert.PowerAssertTest; do
    if [ -d "$project" ]; then
        echo -e "${YELLOW}  📁 Cleaning $project...${NC}"
        rm -rf "$project/bin" "$project/obj" 2>/dev/null || true
    fi
done

# Clear solution-level artifacts
rm -rf bin obj TestResults 2>/dev/null || true

echo -e "${GREEN}✅ All caches and build artifacts cleared${NC}"
echo -e "${YELLOW}💡 Now run: ./publish-local.sh && ./test-local.sh${NC}"
```

Make it executable:
```bash
chmod +x force-refresh.sh
```

#### 4.5 Verification Steps

```bash
# Test package update detection
./publish-local.sh

# Check that packages were updated
ls -la local-feed/*.nupkg | tail -2

# Verify test projects use latest packages
./test-local.sh

# Look for version verification output
```

#### 4.6 Integration Test Sequence

```bash
# Full workflow test with cache issues
./force-refresh.sh
./publish-local.sh  
./test-local.sh

# Should detect and use latest packages without manual intervention
```

#### 4.7 Rollback Plan

```bash
git checkout HEAD -- publish-local.sh
git checkout HEAD -- test-local.sh  
rm -f force-refresh.sh  # if created
```

---

## Complete Implementation Checklist

### Phase 1: Preparation
- [ ] Commit current work: `git add -A && git commit -m "Checkpoint before caching fixes"`
- [ ] Create feature branch: `git checkout -b fix-caching-issues`
- [ ] Backup current state: `git tag backup-before-caching-fixes`

### Phase 2: Implementation (Order Matters)

#### Fix 1: Generated File Cleanup
- [ ] Modify `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets`
- [ ] Add `SharpAssertClean` target
- [ ] Test cleanup: `cd SharpAssert.PackageTest && dotnet build && dotnet clean`
- [ ] Verify: `ls obj/Debug/net9.0/SharpRewritten/` should not exist after clean
- [ ] Commit: `git commit -am "Fix 1: Add generated file cleanup target"`

#### Fix 2: MSBuild Input/Output Tracking
- [ ] Add dependency items to `.targets` file
- [ ] Update `SharpLambdaRewrite` target with `Inputs`/`Outputs`
- [ ] Test incremental build behavior
- [ ] Verify dependency change detection works
- [ ] Commit: `git commit -am "Fix 2: Implement proper MSBuild input/output tracking"`

#### Fix 3: Task Assembly Versioning
- [ ] Modify `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/SharpLambdaRewriteTask.cs`
- [ ] Add assembly timestamp properties and check method
- [ ] Update MSBuild target to pass assembly info
- [ ] Test assembly change detection
- [ ] Commit: `git commit -am "Fix 3: Add task assembly versioning checks"`

#### Fix 4: Package Resolution Updates
- [ ] Modify `/Users/yb/work/oss/SharpAssert/publish-local.sh`
- [ ] Modify `/Users/yb/work/oss/SharpAssert/test-local.sh`
- [ ] Create `/Users/yb/work/oss/SharpAssert/force-refresh.sh`
- [ ] Test package update workflow
- [ ] Commit: `git commit -am "Fix 4: Force package resolution updates in dev workflow"`

### Phase 3: Integration Testing

#### Basic Functionality Test
- [ ] Run: `./force-refresh.sh`
- [ ] Run: `./publish-local.sh`
- [ ] Run: `./test-local.sh`
- [ ] Verify: All tests pass, no caching issues

#### Dev Workflow Test
- [ ] Make trivial change to rewriter logic
- [ ] Run: `./publish-local.sh`
- [ ] Run: `./test-local.sh`
- [ ] Verify: Change is picked up automatically

#### Clean/Build Cycle Test
- [ ] Run: `dotnet clean` (solution level)
- [ ] Verify: All `SharpRewritten/` folders removed
- [ ] Run: `dotnet build`
- [ ] Verify: Clean rebuild works correctly

#### Incremental Build Test
- [ ] Make change to source file in test project
- [ ] Run: `dotnet build`
- [ ] Verify: Only necessary files rebuilt
- [ ] Run: `dotnet build` again
- [ ] Verify: "Up to date" behavior

### Phase 4: Validation & Documentation

#### Performance Validation
- [ ] Time full build: `time dotnet build`
- [ ] Time incremental build: `time dotnet build`
- [ ] Compare to pre-fix timings
- [ ] Ensure no performance regression

#### CI/CD Validation
- [ ] Test with clean environment (remove all local cache)
- [ ] Run: `dotnet restore && dotnet build && dotnet test`
- [ ] Verify: Works without local feed
- [ ] Check: No impact on normal NuGet workflow

#### Update Documentation
- [ ] Add entries to `/Users/yb/work/oss/SharpAssert/learnings.md`
- [ ] Document new scripts and workflow
- [ ] Update any relevant README sections

### Phase 5: Deployment

#### Final Integration Test
- [ ] Run complete test suite: `dotnet test`
- [ ] Run package tests: `./test-local.sh`
- [ ] Run with different MSBuild verbosity levels
- [ ] Test with VS Code and Visual Studio IDE

#### Merge & Clean Up
- [ ] Switch back to main: `git checkout main`
- [ ] Merge feature branch: `git merge fix-caching-issues`
- [ ] Delete feature branch: `git branch -d fix-caching-issues`
- [ ] Remove backup tag: `git tag -d backup-before-caching-fixes`

## Troubleshooting Guide

### Issue: Cleanup target not running
**Solution:** Check MSBuild verbosity: `dotnet clean --verbosity normal`
**Check:** Look for "SharpAssert: Cleaning generated files" message

### Issue: Incremental build not detecting changes
**Solution:** Verify `Inputs`/`Outputs` paths are correct
**Debug:** Use `dotnet build --verbosity diagnostic` and search for "SharpLambdaRewrite"

### Issue: Package updates not picked up
**Solution:** Run `./force-refresh.sh` then `./publish-local.sh`
**Check:** Verify timestamp in package filename matches latest

### Issue: Task assembly versioning not working
**Solution:** Check that task properties are being passed correctly
**Debug:** Look for "Task assembly updated" or "forcing" messages

## Success Criteria

After implementing all fixes:

1. ✅ `dotnet clean` removes all generated files
2. ✅ Changes to rewriter logic trigger regeneration
3. ✅ New dev packages are picked up automatically
4. ✅ MSBuild detects stale task assemblies
5. ✅ Dev workflow (`./test-local.sh`) remains fast and reliable
6. ✅ No impact on normal CI/CD or published package usage
7. ✅ No performance regression in build times
8. ✅ All existing tests continue to pass

## Risk Assessment

**Low Risk:** Fix 1 (cleanup) - Pure additive, no impact on build logic
**Medium Risk:** Fix 2 & 3 (incremental builds, task versioning) - Affects MSBuild behavior
**High Risk:** Fix 4 (package resolution) - Affects dev workflow automation

**Mitigation:** All fixes are behind flags or additive only. Rollback plan provided for each fix.
**Testing:** Each fix is independently testable and committable.