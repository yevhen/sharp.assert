# PowerAssert Integration Plan

## Overview
Add optional PowerAssert backend to SharpAssert, allowing users to choose between the default ExpressionAnalyzer or PowerAssert's visualization engine via an MSBuild property.

## Implementation Checklist

### Phase 1: Core Integration

#### 1.1 Update SharpLambdaRewriteTask.cs
- [ ] Add `UsePowerAssert` property to the MSBuild task class
  ```csharp
  public bool UsePowerAssert { get; set; } = false;
  ```
- [ ] Pass the flag to `SharpAssertRewriter.Rewrite()` method
- [ ] Update logging to include PowerAssert mode status

#### 1.2 Update SharpAssertRewriter.cs
- [ ] Add `usePowerAssert` parameter to `Rewrite()` method signature
- [ ] Pass flag to `SharpAssertSyntaxRewriter` constructor
- [ ] Store flag as field in `SharpAssertSyntaxRewriter` class
- [ ] Modify `CreateInvocationArguments()` to include boolean parameter:
  ```csharp
  // Add as 6th argument after message
  SyntaxFactory.Argument(
      SyntaxFactory.LiteralExpression(
          SyntaxKind.BooleanLiteralExpression,
          usePowerAssert ? SyntaxFactory.Token(SyntaxKind.TrueKeyword) 
                         : SyntaxFactory.Token(SyntaxKind.FalseKeyword)))
  ```

#### 1.3 Update SharpInternal.cs
- [ ] Add PowerAssert NuGet package reference to SharpAssert.csproj
- [ ] Add `usePowerAssert` parameter to `Assert()` method signature (default false)
- [ ] Implement PowerAssert branch:
  ```csharp
  if (usePowerAssert)
  {
      try
      {
          PowerAssert.PAssert.IsTrue(condition);
          return;
      }
      catch (PowerAssert.Infrastructure.ExpressionFailedException ex)
      {
          var failureMessage = message is not null 
              ? $"{message}\n{ex.Message}"
              : ex.Message;
          throw new SharpAssertionException(failureMessage);
      }
  }
  ```
- [ ] Keep existing ExpressionAnalyzer path as default

### Phase 2: MSBuild Integration

#### 2.1 Update SharpAssert.Rewriter.targets
- [ ] Add property group to read `UsePowerAssert` from project:
  ```xml
  <PropertyGroup>
    <_SharpUsePowerAssert Condition="'$(UsePowerAssert)' == 'true'">true</_SharpUsePowerAssert>
    <_SharpUsePowerAssert Condition="'$(UsePowerAssert)' != 'true'">false</_SharpUsePowerAssert>
  </PropertyGroup>
  ```
- [ ] Pass property to SharpLambdaRewriteTask:
  ```xml
  <SharpLambdaRewriteTask
      ...
      UsePowerAssert="$(_SharpUsePowerAssert)" />
  ```

### Phase 3: Test Project Setup

#### 3.1 Create SharpAssert.PowerAssertTest Project
- [ ] Create `SharpAssert.PowerAssertTest/` directory
- [ ] Create `SharpAssert.PowerAssertTest.csproj` with:
  - Target framework: net9.0
  - LangVersion: 13.0
  - UsePowerAssert: true
  - Package references to SharpAssert and SharpAssert.Rewriter from local feed
  - NUnit, FluentAssertions test dependencies

#### 3.2 Create PowerAssertTestFixture.cs
- [ ] Test basic comparison with PowerAssert visualization
- [ ] Test custom message combination
- [ ] Test complex expressions
- [ ] Test passing assertions
- [ ] Test that SharpAssertionException is thrown (not PowerAssert's)
- [ ] Verify PowerAssert output format in error messages

#### 3.3 Update Solution
- [ ] Add SharpAssert.PowerAssertTest to SharpAssert.sln
- [ ] Ensure project dependencies are correctly configured

### Phase 4: Existing Tests

#### 4.1 Update SharpAssert.Rewriter.Tests
- [ ] Add tests for UsePowerAssert flag propagation
- [ ] Test that rewriter adds boolean parameter correctly
- [ ] Test both true and false flag values
- [ ] Verify generated code includes 6th parameter

#### 4.2 Verify SharpAssert.Tests Still Pass
- [ ] Run all existing tests to ensure no regression
- [ ] Verify default behavior (UsePowerAssert=false) unchanged
- [ ] Check that existing Assert calls still work

### Phase 5: Build & Test Scripts

#### 5.1 Update test-local.sh
- [ ] Add PowerAssertTest to test sequence:
  ```bash
  echo "🧪 Testing PowerAssert integration..."
  dotnet test SharpAssert.PowerAssertTest/ -v n
  ```
- [ ] Ensure script fails if PowerAssert tests fail

#### 5.2 Update publish-local.sh (if needed)
- [ ] Verify PowerAssert dependency is included in package
- [ ] Check package metadata is correct

### Phase 6: Documentation

#### 6.1 Update README.md
- [ ] Add PowerAssert option to features section
- [ ] Document how to enable PowerAssert mode
- [ ] Show example of PowerAssert output
- [ ] Add to troubleshooting if needed

#### 6.2 Update CLAUDE.md
- [ ] Document PowerAssert integration in learnings
- [ ] Add any gotchas or considerations

### Phase 7: Validation

#### 7.1 End-to-End Testing
- [ ] Create fresh test project
- [ ] Install packages from local feed
- [ ] Enable UsePowerAssert in project file
- [ ] Verify PowerAssert visualizations appear
- [ ] Test with UsePowerAssert=false (default behavior)

#### 7.2 Performance Testing
- [ ] Compare build times with/without PowerAssert
- [ ] Check runtime performance impact
- [ ] Verify no memory leaks

## Success Criteria

✅ Users can opt-in to PowerAssert via `<UsePowerAssert>true</UsePowerAssert>`
✅ Default behavior unchanged when property not set
✅ Custom messages work with PowerAssert
✅ All tests pass in both modes
✅ Consistent SharpAssertionException thrown
✅ No breaking changes to existing API

## Testing Commands

```bash
# Test default mode
dotnet test SharpAssert.Tests/
dotnet test SharpAssert.PackageTest/

# Test PowerAssert mode
dotnet test SharpAssert.PowerAssertTest/

# Full validation
./test-local.sh
```

## Rollback Plan

If issues arise:
1. Remove UsePowerAssert parameter from SharpInternal.Assert
2. Remove PowerAssert package reference
3. Revert rewriter changes
4. Remove PowerAssertTest project

The changes are additive and behind a flag, minimizing risk to existing users.