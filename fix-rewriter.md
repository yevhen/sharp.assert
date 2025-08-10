# Fix SharpAssert Rewriter - Dual-World MSBuild Configuration

## Problem Statement
The current rewriter breaks IDE experience by removing original files from compilation, causing:
- Loss of IntelliSense in original source files
- Broken refactoring and navigation
- Debugging issues (breakpoints don't work)
- IDE treats original files as non-compilable

## Solution: Professional "Dual-World" MSBuild Configuration
Implement the same approach used by C# compiler team for Razor and source generators.

## Implementation Checklist

### Phase 1: MSBuild Target Modifications ✅ COMPLETED
- [x] Update `SharpAssert.Rewriter/build/SharpAssert.Rewriter.targets`
  - [x] Add `Condition="'$(DesignTimeBuild)' != 'true'"` to SharpLambdaRewrite target
  - [x] Add `Condition="'$(BuildingForLiveUnitTesting)' != 'true'"` for Live Unit Testing support  
  - [x] Keep original Compile items intact (don't remove them in design-time)
  - [x] Create ItemGroup for tracking rewritten files
  - [x] Only swap Compile items during actual build (not design-time)

### Phase 2: #line Directive Implementation ✅ COMPLETED
- [x] Modify `SharpAssert.Rewriter/SharpAssertRewriter.cs`
  - [x] Track original source file path for each syntax tree
  - [x] Add #line directive at start of generated file: `#line 1 "original/path.cs"`
  - [x] Before each rewritten Assert, add: `#line <original-line> "original/path.cs"`
  - [x] After each rewritten Assert, add: `#line default`
  - [x] Ensure proper escaping of file paths in #line directives

### Phase 3: Rewriter Task Enhancement ✅ COMPLETED
- [x] Update `SharpAssert.Rewriter/SharpLambdaRewriteTask.cs`
  - [x] Pass original file paths to the rewriter (already done in Phase 2)
  - [x] Maintain mapping between original and rewritten files
  - [x] Add diagnostic output for troubleshooting (when verbose logging enabled)
  - [x] Handle edge cases (files without Assert calls, generated files)

### Phase 4: Line Number Tracking ✅ COMPLETED
- [x] Update `SharpAssert.Rewriter/SharpAssertSyntaxRewriter.cs`
  - [x] Extract original line numbers from syntax nodes
  - [x] Pass line numbers through rewriting pipeline
  - [x] Ensure line numbers are preserved in generated Assert calls
  - [x] Handle multi-line Assert expressions correctly

### Phase 6: Edge Cases & Polish
- [ ] Handle files with existing #line directives
- [ ] Support for partial classes across multiple files
- [ ] Ensure compatibility with source generators
- [ ] Test with async/await in Assert expressions

### Phase 8: Documentation
- [ ] Update README with IDE compatibility notes
- [ ] Add troubleshooting guide for common issues

## Expected Outcome

### Design-Time (IDE View)
```
Original File: MyTest.cs
├── Full IntelliSense support
├── Refactoring works
├── Navigation works
└── Breakpoints work
```

### Compile-Time (Build View)
```
Rewritten File: obj/Debug/SharpRewritten/MyTest.cs.sharp.g.cs
├── Contains #line directives
├── Points back to original source
├── Used for actual compilation
└── Hidden from IDE
```

## Technical Details

### MSBuild Properties to Check
- `$(DesignTimeBuild)` - true when IDE is building for IntelliSense
- `$(BuildingForLiveUnitTesting)` - true during Live Unit Testing
- `$(BuildingInsideVisualStudio)` - true when building in VS
- `$(BuildingProject)` - false during design-time builds

### Example Generated Code with #line Directives
```csharp
#line 1 "C:\Project\Tests\MyTest.cs"
using static Sharp;
using NUnit.Framework;

[TestFixture]
public class MyTests
{
    [Test]
    public void TestMethod()
    {
        var x = 1;
        var y = 2;
#line 12 "C:\Project\Tests\MyTest.cs"
        global::SharpAssert.SharpInternal.Assert(()=>x == y, "x == y", @"C:\Project\Tests\MyTest.cs", 12);
#line default
    }
}
```

## Success Criteria
- [ ] IDE shows no errors in original source files
- [ ] IntelliSense works perfectly in original files
- [ ] Debugging breakpoints hit in original source
- [ ] Stack traces show original file locations
- [ ] Compiler errors point to original source
- [ ] Build succeeds in both IDE and CLI
- [ ] No performance degradation in IDE

## Notes
- This approach is used by production systems like Razor and source generators
- The #line directive has been part of C# since 1.0 and is well-supported
- Design-time builds happen frequently in IDEs, so performance is critical
- Must test with all major IDEs to ensure compatibility