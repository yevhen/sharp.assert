# PowerAssert Integration Plan

## Overview
Add PowerAssert as both an optional backend and automatic fallback for unsupported features, enabling early package release while continuing development.

### Strategy
1. **UsePowerAssert** - Force PowerAssert for ALL assertions (default: false)
2. **UsePowerAssertForUnsupportedFeatures** - Auto-fallback to PowerAssert for features not yet implemented (default: true)

This allows shipping a fully functional package immediately, with progressive enhancement as features are implemented.

## Implementation Checklist

### Phase 1: Core Integration

#### 1.1 Update SharpLambdaRewriteTask.cs
- [ ] Add `UsePowerAssert` property to the MSBuild task class
  ```csharp
  public bool UsePowerAssert { get; set; } = false;
  ```
- [ ] Add `UsePowerAssertForUnsupportedFeatures` property
  ```csharp
  public bool UsePowerAssertForUnsupportedFeatures { get; set; } = true;
  ```
- [ ] Pass both flags to `SharpAssertRewriter.Rewrite()` method
- [ ] Update logging to include PowerAssert mode status

#### 1.2 Update SharpAssertRewriter.cs
- [ ] Add `usePowerAssert` and `usePowerAssertForUnsupported` parameters to `Rewrite()` method signature
- [ ] Pass both flags to `SharpAssertSyntaxRewriter` constructor
- [ ] Store both flags as fields in `SharpAssertSyntaxRewriter` class
- [ ] Modify `CreateInvocationArguments()` to include both boolean parameters:
  ```csharp
  // Add as 6th and 7th arguments after message
  SyntaxFactory.Argument(
      SyntaxFactory.LiteralExpression(
          SyntaxKind.BooleanLiteralExpression,
          usePowerAssert ? SyntaxFactory.Token(SyntaxKind.TrueKeyword) 
                         : SyntaxFactory.Token(SyntaxKind.FalseKeyword))),
  SyntaxFactory.Argument(
      SyntaxFactory.LiteralExpression(
          SyntaxKind.BooleanLiteralExpression,
          usePowerAssertForUnsupported ? SyntaxFactory.Token(SyntaxKind.TrueKeyword) 
                                       : SyntaxFactory.Token(SyntaxKind.FalseKeyword)))
  ```

#### 1.3 Update SharpInternal.cs
- [ ] Add PowerAssert NuGet package reference to SharpAssert.csproj
- [ ] Add `usePowerAssert` and `usePowerAssertForUnsupported` parameters to `Assert()` method signature
- [ ] Create `UnsupportedFeatureDetector` class to detect unsupported features:
  ```csharp
  class UnsupportedFeatureDetector : ExpressionVisitor
  {
      public bool HasUnsupported { get; private set; }
      
      protected override Expression VisitMethodCall(MethodCallExpression node)
      {
          // LINQ methods: Contains, Any, All, SequenceEqual
          var methodName = node.Method.Name;
          if (methodName is "Contains" or "Any" or "All" or "SequenceEqual")
          {
              HasUnsupported = true;
          }
          return base.VisitMethodCall(node);
      }
      
      protected override Expression VisitBinary(BinaryExpression node)
      {
          // String comparisons (need DiffPlex)
          if (node.Left.Type == typeof(string) && node.Right.Type == typeof(string))
              HasUnsupported = true;
          
          // Collection comparisons
          if (IsCollection(node.Left.Type) || IsCollection(node.Right.Type))
              HasUnsupported = true;
          
          // Complex object comparisons
          if (IsComplexType(node.Left.Type) || IsComplexType(node.Right.Type))
              HasUnsupported = true;
          
          return base.VisitBinary(node);
      }
  }
  ```
- [ ] Implement PowerAssert fallback logic:
  ```csharp
  // Force PowerAssert if flag is set
  if (usePowerAssert)
      return UsePowerAssert(condition, message);
  
  // Check for unsupported features
  if (usePowerAssertForUnsupported && HasUnsupportedFeatures(condition))
      return UsePowerAssert(condition, message);
  
  // Use SharpAssert's analyzer
  var analyzer = new ExpressionAnalyzer();
  // ... existing logic
  ```
- [ ] Implement `UsePowerAssert` helper method:
  ```csharp
  static void UsePowerAssert(Expression<Func<bool>> condition, string? message)
  {
      try
      {
          PowerAssert.PAssert.IsTrue(condition);
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
- [ ] Keep existing ExpressionAnalyzer path as default for supported features

### Phase 2: MSBuild Integration

#### 2.1 Update SharpAssert.Rewriter.targets
- [ ] Add property group to read both properties from project:
  ```xml
  <PropertyGroup>
    <_SharpUsePowerAssert Condition="'$(UsePowerAssert)' == 'true'">true</_SharpUsePowerAssert>
    <_SharpUsePowerAssert Condition="'$(UsePowerAssert)' != 'true'">false</_SharpUsePowerAssert>
    
    <_SharpUsePowerAssertForUnsupported Condition="'$(UsePowerAssertForUnsupportedFeatures)' == 'false'">false</_SharpUsePowerAssertForUnsupported>
    <_SharpUsePowerAssertForUnsupported Condition="'$(UsePowerAssertForUnsupportedFeatures)' != 'false'">true</_SharpUsePowerAssertForUnsupported>
  </PropertyGroup>
  ```
- [ ] Pass both properties to SharpLambdaRewriteTask:
  ```xml
  <SharpLambdaRewriteTask
      ...
      UsePowerAssert="$(_SharpUsePowerAssert)"
      UsePowerAssertForUnsupportedFeatures="$(_SharpUsePowerAssertForUnsupported)" />
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

#### 3.3 Create FallbackTestFixture.cs
- [ ] Test fallback to PowerAssert for string comparisons
- [ ] Test fallback for LINQ Contains method
- [ ] Test fallback for collection comparisons
- [ ] Test fallback for complex object comparisons
- [ ] Test that supported features still use SharpAssert analyzer
- [ ] Test with UsePowerAssertForUnsupportedFeatures=false disables fallback

#### 3.4 Update Solution
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
✅ Automatic fallback to PowerAssert for unsupported features (enabled by default)
✅ Default behavior unchanged for supported features
✅ Custom messages work with PowerAssert
✅ All tests pass in both modes
✅ Consistent SharpAssertionException thrown
✅ No breaking changes to existing API
✅ Package can be released immediately with full functionality

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

## Implementation Stages

### Stage 1: Immediate Release (With Fallback)
- Ship with `UsePowerAssertForUnsupportedFeatures=true` by default
- All unsupported features automatically use PowerAssert
- Supported features (basic comparisons, logical operators) use SharpAssert

### Stage 2: Progressive Enhancement
As each feature is implemented in SharpAssert:
1. String comparisons with DiffPlex (Increment 5)
2. Collection comparisons (Increment 6)
3. Object deep comparison (Increment 7)
4. LINQ operations (Increment 8)
5. SequenceEqual (Increment 9)

Each completed feature automatically switches from PowerAssert to SharpAssert.

### Stage 3: Feature Complete
- Consider making `UsePowerAssertForUnsupportedFeatures=false` the default
- PowerAssert becomes purely optional via `UsePowerAssert` flag
- Potentially remove PowerAssert dependency in major version

## Rollback Plan

If issues arise:
1. Remove UsePowerAssert parameters from SharpInternal.Assert
2. Remove PowerAssert package reference
3. Revert rewriter changes
4. Remove PowerAssertTest project

The changes are additive and behind flags, minimizing risk to existing users.