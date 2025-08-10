# MSBuild Task Testing - TDD Implementation Plan

## Overview
Test SharpLambdaRewriteTask using a simplified two-pronged approach:
1. **Primary**: Direct unit testing of the MSBuild task (fast, comprehensive)
2. **Secondary**: Simple manual integration project for .targets verification

## Testing Strategy

### Primary: Direct Task Unit Testing
Add comprehensive unit tests to `SharpAssert.Rewriter.Tests` that test `SharpLambdaRewriteTask` directly.

**Benefits**: Fast execution, easy debugging, comprehensive coverage of task logic

### Secondary: Manual Integration Project  
Create a simple project that uses the rewriter via .targets integration for manual verification.

**Project Structure**:
```
SharpAssert.IntegrationTest/
├── SharpAssert.IntegrationTest.csproj
├── TestFile.cs                    # Simple Assert(x == y) calls
└── README.md                      # Manual testing instructions
```

## Implementation Tasks

### Phase 1: Direct Task Unit Tests
- [x] **Step 1: Basic Task Execution**
  - [x] Create failing test for SharpLambdaRewriteTask.Execute()
  - [x] Test task with simple Assert() source files
  - [x] Verify output files are created with correct content
  - [x] Test task returns true on success

- [x] **Step 2: File Processing Logic**
  - [x] Test files with Assert calls are rewritten
  - [x] Test files without Assert calls are copied unchanged
  - [x] Test multiple source files in single execution
  - [x] Verify correct output path generation

- [x] **Step 3: Edge Cases & Error Handling**
  - [x] Test async Assert detection (should skip rewriting)
  - [x] Test invalid syntax fallback behavior
  - [x] Test empty/null source inputs
  - [x] Test exception handling and graceful failures

- [x] **Step 4: Configuration & Properties**
  - [x] Test LangVersion parameter handling
  - [x] Test NullableContext parameter handling
  - [x] Test logging output with different MessageImportance levels

### Phase 2: Manual Integration Project
- [ ] **Step 5: Create Integration Project**
  - [ ] Create SharpAssert.IntegrationTest.csproj with rewriter reference
  - [ ] Add simple TestFile.cs with Assert calls
  - [ ] Add README.md with manual testing instructions
  - [ ] Add project to solution file

- [ ] **Step 6: Manual Testing Documentation**
  - [ ] Document build command (`dotnet build`)
  - [ ] Document verification steps (check obj/Debug/net9.0/SharpRewritten/)
  - [ ] Document expected output patterns
  - [ ] Document troubleshooting steps

## Test Implementation Details

### Direct Task Unit Tests
```csharp
[TestFixture]
public class SharpLambdaRewriteTaskFixture
{
    [Test]
    public void Should_rewrite_assert_calls()
    {
        var tempDir = CreateTempDirectory();
        var sourceFile = CreateSourceFile(tempDir, "Assert(x == y);");
        
        var task = new SharpLambdaRewriteTask 
        {
            Sources = new[] { CreateTaskItem(sourceFile) },
            ProjectDir = tempDir,
            OutputDir = Path.Combine(tempDir, "output")
        };
        
        var result = task.Execute();
        
        result.Should().BeTrue();
        var outputFile = Path.Combine(tempDir, "output", "test.cs.sharp.g.cs");
        var content = File.ReadAllText(outputFile);
        content.Should().Contain("SharpInternal.Assert(() =>");
    }
}
```

### Manual Integration Project
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <EnableSharpLambdaRewrite>true</EnableSharpLambdaRewrite>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="../SharpAssert.Rewriter/SharpAssert.Rewriter.csproj" />
  </ItemGroup>
</Project>
```

## Success Criteria
- ✅ Task logic comprehensively unit tested (fast feedback loop)
- ✅ All edge cases and error scenarios covered in unit tests
- ✅ Manual integration project proves .targets integration works
- ✅ Simple debugging and development workflow maintained
- ✅ No complex MSBuild orchestration or assembly loading issues

## Benefits of This Approach
- **Fast Development**: Unit tests provide immediate feedback
- **Simple Debugging**: Clear separation between task logic and integration
- **Comprehensive Coverage**: All task behavior tested without complexity
- **Manual Verification**: Easy to verify .targets integration works end-to-end
- **Maintainable**: No complex test infrastructure to maintain