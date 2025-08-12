# Fix Caching Issues - Updated Analysis

**Status:** Updated based on package-testing-implementation.md completion

## Current Status After Package Testing Implementation

The package-testing-implementation.md has been ✅ **COMPLETED** with the following infrastructure in place:

- ✅ SharpAssert.IntegrationTests project created and working
- ✅ SharpAssert.PackageTesting.sln separate solution created
- ✅ test-local.sh script with isolated package testing 
- ✅ nuget.package-tests.config for isolated NuGet feeds
- ✅ dev-test.sh for development testing
- ✅ Basic package workflow functional

This addresses 2 critical issues that need to be resolved for optimal development workflow.

## Remaining Critical Issues

Based on actual testing, these issues still need to be addressed:

1. **Generated file cleanup** - ❌ CONFIRMED: `dotnet clean` doesn't remove `SharpRewritten/` folder
2. **MSBuild input/output tracking** - ⚠️ NEEDS TESTING: Incremental builds may miss rewriter assembly changes

---

## Fix 1: Add Generated File Cleanup Target

**Problem:** ❌ **CONFIRMED** - `dotnet clean` doesn't remove the `SharpRewritten/` folder, leaving stale generated files.

**Impact:** Stale files can cause confusing build behavior and mask real issues.

**Testing Results:** 
- ✅ Generated files exist after build in `obj/Debug/net9.0/SharpRewritten/`
- ❌ `dotnet clean` leaves SharpRewritten folder and files behind

### Implementation

#### 1.1 Add Cleanup Target to MSBuild

**File:** `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets`

**Change:** Add cleanup target after line 59 (end of file):

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
# Test cleanup in SharpAssert.IntegrationTests (use project that works)
cd SharpAssert.IntegrationTests
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

**Problem:** ⚠️ **NEEDS TESTING** - Incremental builds may not detect when rewriter assembly changes, leading to stale generated files.

**Impact:** Changes to rewriter logic might not trigger regeneration of source files.

**Current State:** The current `.targets` file does NOT have proper `Inputs`/`Outputs` attributes on the `SharpLambdaRewrite` target.

### Implementation (If Needed After Testing)

#### 2.1 Test Current Behavior First

```bash
# Test if incremental builds detect rewriter changes
cd SharpAssert.IntegrationTests
dotnet build

# Simulate rewriter update
touch ../SharpAssert.Rewriter/bin/Debug/net9.0/SharpAssert.Rewriter.dll

# Check if rebuild is triggered
dotnet build --verbosity normal | grep -i "sharp"
```

#### 2.2 Add Input Dependencies (Only if test above fails)

**File:** `/Users/yb/work/oss/SharpAssert/SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets`

**Change:** Modify the `SharpLambdaRewrite` target (line 16) to add `Inputs`/`Outputs`:

```xml
<Target Name="SharpLambdaRewrite" BeforeTargets="CoreCompile"
        Condition="'$(EnableSharpLambdaRewrite)'=='true' and '$(DesignTimeBuild)' != 'true' and '$(BuildingForLiveUnitTesting)' != 'true'"
        Inputs="@(_SharpSourceFiles);$(SharpAssertRewriterPath)"
        Outputs="@(_SharpSourceFiles->'$(IntermediateOutputPath)SharpRewritten\%(RecursiveDir)%(Filename)%(Extension).sharp.g.cs')">
```

## Updated Implementation Plan

### Priority Order (Based on Current Analysis)

#### High Priority: Fix 1 - Generated File Cleanup ✅ **COMPLETED**
- [x] Add `SharpAssertClean` target to `.targets` file
- [x] Test cleanup functionality
- [x] Commit fix

#### Medium Priority: Fix 2 - Testing Required ⚠️ NEEDS ASSESSMENT
- [ ] Test current incremental build behavior
- [ ] Implement fix only if issues are confirmed

### Simplified Success Criteria

After implementing remaining fixes:

1. ✅ `dotnet clean` removes all generated files (Fix 1)
2. ⚠️ Changes to rewriter logic trigger regeneration (Test Fix 2)
3. ✅ Dev workflow (`./test-local.sh`) remains fast and reliable (Already working)
4. ✅ No impact on normal CI/CD or published package usage (Already working)

### Next Steps

1. **Immediately implement Fix 1** (confirmed issue, low risk)
2. **Test behavior for Fix 2** before implementing
3. **Update learnings.md** with findings
4. **Consider this analysis complete** if testing shows Fix 2 is not needed