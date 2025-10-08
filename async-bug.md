# Post-Mortem: Async Assertion Rewriting Bug

**Date**: 2025-10-08
**Severity**: Critical
**Status**: Fixed
**Component**: MSBuild Rewriter (SharpAssertRewriter.cs)

## Executive Summary

Async assertions were not being awaited in the rewritten code, causing assertion methods to return unawaited `Task` objects that never executed. This resulted in all async assertions silently passing regardless of the actual condition value.

## The Bug

### Symptoms

When running async demos from the SharpAssert.Demo project:

```
Demo: Basic Await
Description: Simple async condition
--------------------------------------------------------------------------------
ERROR: Demo should have failed but didn't!
```

All async assertions compiled without errors but never threw exceptions, even when the condition was `false`.

### Root Cause

The rewriter generated calls to `SharpInternal.AssertAsync()` and `SharpInternal.AssertAsyncBinary()` but failed to wrap them in `await` expressions.

**Incorrect generated code:**
```csharp
public static async Task BasicAwaitCondition()
{
    global::SharpAssert.SharpInternal.AssertAsync(
        async()=>await GetBoolAsync(),
        "await GetBoolAsync()",
        "/path/to/file.cs",
        36)
    ;  // ← Missing await! Task returned but never awaited
}
```

**Correct generated code:**
```csharp
public static async Task BasicAwaitCondition()
{
    await global::SharpAssert.SharpInternal.AssertAsync(
        async()=>await GetBoolAsync(),
        "await GetBoolAsync()",
        "/path/to/file.cs",
        36)
    ;  // ✓ Properly awaited
}
```

## Investigation Timeline

### 1. Initial Discovery: Missing Rich Diagnostics

First symptom discovered while running demo:
```
Demo: Complex Expression
Description: Multi-variable expression with operators
--------------------------------------------------------------------------------
Assertion failed: x + y * z > 100  at /Users/.../01_BasicAssertionsDemos.cs:39
```

**Problem**: No evaluated values shown! Should have displayed:
```
  Left:  25
  Right: 100
```

**Cause**: Demo project was missing MSBuild rewriter integration entirely.

### 2. First Fix: MSBuild Integration

Added to `SharpAssert.Demo.csproj`:
```xml
<PropertyGroup>
  <!-- Override the rewriter assembly path for local development -->
  <SharpAssertRewriterPath>$(MSBuildThisFileDirectory)..\SharpAssert\bin\$(Configuration)\net9.0\SharpAssert.dll</SharpAssertRewriterPath>
</PropertyGroup>

<ItemGroup>
  <!-- Reference SharpAssert.Runtime normally -->
  <ProjectReference Include="..\SharpAssert.Runtime\SharpAssert.Runtime.csproj" />

  <!-- Build the rewriter project but don't reference its assembly -->
  <ProjectReference Include="..\SharpAssert\SharpAssert.csproj"
                    ReferenceOutputAssembly="false" />
</ItemGroup>

<!-- Import the targets file to enable MSBuild task -->
<Import Project="..\SharpAssert\build\SharpAssert.targets" />
```

This enabled rewriting, but exposed the async bug.

### 3. Async/Dynamic Detection Issues

During initial implementation, discovered the rewriter lacked proper detection for:
- **Async expressions**: Needed to detect `await` in conditions
- **Dynamic expressions**: Needed to detect `dynamic` type operations

**Added detection methods:**
```csharp
static bool ContainsAwait(InvocationExpressionSyntax node) =>
    node.DescendantNodes()
        .OfType<AwaitExpressionSyntax>()
        .Any();

bool ContainsDynamic(InvocationExpressionSyntax node)
{
    var conditionArgument = node.ArgumentList.Arguments[0];
    var typeInfo = semanticModel.GetTypeInfo(conditionArgument.Expression);

    if (typeInfo.Type?.TypeKind == TypeKind.Dynamic)
        return true;

    // Check for dynamic operations in sub-expressions
    return conditionArgument.DescendantNodes()
        .OfType<ExpressionSyntax>()
        .Any(expr =>
        {
            var exprTypeInfo = semanticModel.GetTypeInfo(expr);
            return exprTypeInfo.Type?.TypeKind == TypeKind.Dynamic;
        });
}
```

### 4. Async Thunk Generation Bug

**Problem**: `CreateAsyncThunk()` only checked descendants, missing top-level `AwaitExpressionSyntax`:

```csharp
// WRONG: Misses `await GetLeftValueAsync()` because the operand IS the await
var containsAwait = operand.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
```

**Fixed version:**
```csharp
var containsAwait = operand is AwaitExpressionSyntax ||
                   operand.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
```

This caused errors like:
```
error CS4034: The 'await' operator can only be used within an async lambda expression.
```

### 5. Missing Await Wrapper - The Core Bug

**Original code returned `InvocationExpressionSyntax`:**
```csharp
InvocationExpressionSyntax RewriteToAsync(InvocationExpressionSyntax node)
{
    var rewriteData = ExtractRewriteData(node);
    var asyncLambda = CreateAsyncLambda(rewriteData.Expression);
    var newInvocation = CreateAsyncInvocation(asyncLambda, rewriteData);
    return AddLineDirectives(newInvocation, node, rewriteData.LineNumber);
}
```

This generated: `global::SharpAssert.SharpInternal.AssertAsync(...)` without `await`.

**Fixed version returns `AwaitExpressionSyntax`:**
```csharp
AwaitExpressionSyntax RewriteToAsync(InvocationExpressionSyntax node)
{
    var rewriteData = ExtractRewriteData(node);
    var asyncLambda = CreateAsyncLambda(rewriteData.Expression);
    var newInvocation = CreateAsyncInvocation(asyncLambda, rewriteData);
    var awaitExpr = SyntaxFactory.AwaitExpression(
        SyntaxFactory.Token(
            SyntaxFactory.TriviaList(),
            SyntaxKind.AwaitKeyword,
            SyntaxFactory.TriviaList(SyntaxFactory.Space)),  // ← Critical: space after await
        newInvocation);

    return AddLineDirectivesToAwait(awaitExpr, node, rewriteData.LineNumber);
}
```

### 6. Line Directive Placement Bug

**Problem**: Line directives embedded in `await` expression caused:
```
error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
```

Generated code looked like:
```csharp
await #line 36 "/path/to/file.cs"
global::SharpAssert.SharpInternal.AssertAsync(...)
```

**Solution**: Move line directives to the await expression level:
```csharp
AwaitExpressionSyntax AddLineDirectivesToAwait(
    AwaitExpressionSyntax awaitExpr,
    InvocationExpressionSyntax originalNode,
    int lineNumber) =>
    awaitExpr
        .WithLeadingTrivia(CreateLeadingTrivia(originalNode, lineNumber))
        .WithTrailingTrivia(CreateTrailingTrivia(originalNode));
```

Generates:
```csharp
#line 36 "/path/to/file.cs"
await global::SharpAssert.SharpInternal.AssertAsync(...)
#line default
```

### 7. Spacing Bug: "awaitglobal" Concatenation

**Problem**: Without explicit spacing, generated:
```csharp
awaitglobal::SharpAssert.SharpInternal.AssertAsync(...)
```

Causing:
```
error CS0432: Alias 'awaitglobal' not found
```

**Solution**: Add space trivia after await keyword:
```csharp
SyntaxFactory.AwaitExpression(
    SyntaxFactory.Token(
        SyntaxFactory.TriviaList(),
        SyntaxKind.AwaitKeyword,
        SyntaxFactory.TriviaList(SyntaxFactory.Space)),  // ← Space here
    newInvocation)
```

## The Complete Fix

### Updated Rewriter Logic

```csharp
public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
{
    if (!IsSharpAssertCall(node))
        return base.VisitInvocationExpression(node);

    var conditionArgument = node.ArgumentList.Arguments[0];
    var hasAwait = ContainsAwait(node);
    var hasDynamic = ContainsDynamic(node);
    var isBinary = IsBinaryOperation(conditionArgument.Expression);

    HasRewrites = true;

    // Priority: await > dynamic (per PRD section 4.2)
    if (hasAwait)
        return isBinary ? RewriteToAsyncBinary(node) : RewriteToAsync(node);

    if (hasDynamic)
        return isBinary ? RewriteToDynamicBinary(node) : RewriteToDynamic(node);

    return RewriteToLambda(node);
}
```

### Five Rewrite Paths

1. **Sync**: `Assert(condition)` → `SharpInternal.Assert(() => condition, ...)`
2. **Async + non-binary**: `Assert(await expr)` → `await SharpInternal.AssertAsync(async () => await expr, ...)`
3. **Async + binary**: `Assert(await x == await y)` → `await SharpInternal.AssertAsyncBinary(async () => await x, async () => await y, BinaryOp.Eq, ...)`
4. **Dynamic + non-binary**: `Assert(dynamic expr)` → `SharpInternal.AssertDynamic(() => expr, ...)`
5. **Dynamic + binary**: `Assert(dynamic x == y)` → `SharpInternal.AssertDynamicBinary(() => (object?)x, () => (object?)y, BinaryOp.Eq, ...)`

### Helper Methods Added

```csharp
// Create async lambda wrapper
static ParenthesizedLambdaExpressionSyntax CreateAsyncLambda(ExpressionSyntax operand) =>
    SyntaxFactory.ParenthesizedLambdaExpression()
        .WithAsyncKeyword(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
        .WithParameterList(SyntaxFactory.ParameterList())
        .WithExpressionBody(operand);

// Create async thunk for operands
ParenthesizedLambdaExpressionSyntax CreateAsyncThunk(ExpressionSyntax operand)
{
    var containsAwait = operand is AwaitExpressionSyntax ||
                       operand.DescendantNodes().OfType<AwaitExpressionSyntax>().Any();
    return containsAwait ? CreateAsyncLambda(operand) : WrapInTaskFromResult(operand);
}

// Create dynamic thunk for operands
static ParenthesizedLambdaExpressionSyntax CreateDynamicThunk(ExpressionSyntax operand)
{
    var castToObject = SyntaxFactory.CastExpression(
        SyntaxFactory.NullableType(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))),
        operand);

    return SyntaxFactory.ParenthesizedLambdaExpression()
        .WithParameterList(SyntaxFactory.ParameterList())
        .WithExpressionBody(castToObject);
}

// Wrap sync operands in Task.FromResult
static ParenthesizedLambdaExpressionSyntax WrapInTaskFromResult(ExpressionSyntax operand)
{
    var objectType = SyntaxFactory.NullableType(
        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)));

    var taskFromResult = SyntaxFactory.InvocationExpression(
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("Task"),
            SyntaxFactory.GenericName("FromResult")
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(objectType)))))
        .WithArgumentList(SyntaxFactory.ArgumentList(
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(operand))));

    return SyntaxFactory.ParenthesizedLambdaExpression()
        .WithParameterList(SyntaxFactory.ParameterList())
        .WithExpressionBody(taskFromResult);
}
```

## Testing Gaps Identified

### 1. Incorrect Rewriter Test

**File**: `src/SharpAssert.Tests/Rewriter/RewriterFixture.cs:93-114`

**Current test** (WRONG):
```csharp
[Test]
public void Should_skip_rewrite_if_async_present()
{
    var source = """
        using static SharpAssert.Sharp;

        class Test
        {
            async Task Method()
            {
                Assert(await GetBoolAsync());
            }

            Task<bool> GetBoolAsync() => Task.FromResult(true);
        }
        """;

    var expected = source; // NO! This is wrong!

    var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

    result.Should().Be(expected);
}
```

**Problem**: Test expects async code to be skipped, but it should be rewritten to `AssertAsync`.

**Correct test should verify**:
```csharp
[Test]
public void Should_rewrite_async_to_AssertAsync()
{
    var source = """
        using static SharpAssert.Sharp;

        class Test
        {
            async Task Method()
            {
                Assert(await GetBoolAsync());
            }

            Task<bool> GetBoolAsync() => Task.FromResult(true);
        }
        """;

    var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

    result.Should().Contain("await global::SharpAssert.SharpInternal.AssertAsync");
    result.Should().Contain("async()=>await GetBoolAsync()");
}
```

### 2. Missing Integration Tests

**File**: `src/SharpAssert.IntegrationTests/AsyncIntegrationFixture.cs`

**Current tests** only verify basic success/failure but don't test:
- Binary comparisons with await on both sides
- Mixed async/sync comparisons
- Async string diffs
- Async collection comparisons
- Await expression detection edge cases

**Needed tests**:
```csharp
[Test]
public async Task Should_handle_async_binary_comparison()
{
    var action = async () => Assert(await GetLeftAsync() == await GetRightAsync());

    var exception = await action.Should().ThrowAsync<SharpAssertionException>();
    exception.Which.Message.Should().Contain("Left:  42");
    exception.Which.Message.Should().Contain("Right: 100");
}

[Test]
public async Task Should_handle_mixed_async_sync_comparison()
{
    var action = async () => Assert(await GetLeftAsync() == 100);

    var exception = await action.Should().ThrowAsync<SharpAssertionException>();
    exception.Which.Message.Should().Contain("Left:  42");
    exception.Which.Message.Should().Contain("Right: 100");
}

[Test]
public async Task Should_handle_async_string_diff()
{
    var action = async () => Assert(await GetStringAsync() == "expected");

    var exception = await action.Should().ThrowAsync<SharpAssertionException>();
    exception.Which.Message.Should().Contain("Diff:");
}

static async Task<int> GetLeftAsync()
{
    await Task.Delay(1);
    return 42;
}

static async Task<int> GetRightAsync()
{
    await Task.Delay(1);
    return 100;
}

static async Task<string> GetStringAsync()
{
    await Task.Delay(1);
    return "actual";
}
```

### 3. Missing Rewriter Output Validation

Should add tests that verify the exact generated code:
```csharp
[Test]
public void Should_generate_await_keyword_for_async_assertions()
{
    var source = """
        using static SharpAssert.Sharp;

        class Test
        {
            async Task Method()
            {
                Assert(await GetBoolAsync());
            }
        }
        """;

    var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

    // Verify await is present
    result.Should().Contain("await global::SharpAssert.SharpInternal.AssertAsync");

    // Verify no concatenation bug
    result.Should().NotContain("awaitglobal::");

    // Verify proper spacing
    result.Should().Match("*await global::SharpAssert*");
}

[Test]
public void Should_generate_proper_line_directives_for_await()
{
    var source = """
        using static SharpAssert.Sharp;

        class Test
        {
            async Task Method()
            {
                Assert(await GetBoolAsync());
            }
        }
        """;

    var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

    // Line directive should be before await, not embedded
    result.Should().Match("*#line*await*");
    result.Should().NotMatch("*await*#line*global::SharpAssert*");
}
```

### 4. Missing Dynamic Tests

No rewriter tests for dynamic expressions at all. Should add:
```csharp
[Test]
public void Should_rewrite_dynamic_binary_comparison()
{
    var source = """
        using static SharpAssert.Sharp;

        class Test
        {
            void Method()
            {
                dynamic x = 42;
                Assert(x == 100);
            }
        }
        """;

    var result = SharpAssertRewriter.Rewrite(source, "TestFile.cs");

    result.Should().Contain("global::SharpAssert.SharpInternal.AssertDynamicBinary");
}
```

## Verification

### Demo Output After Fix

```
Demo: Async Binary
Description: Both sides awaited with values shown
--------------------------------------------------------------------------------
Assertion failed: await GetLeftValueAsync() == await GetRightValueAsync()
  Left:  42
  Right: 100

Demo: Async String Diff
Description: Awaited string with diff
--------------------------------------------------------------------------------
Assertion failed: await GetStringAsync() == "expected value"
  Left:  "actual value"
  Right: "expected value"
  Diff: [-a][+expe]ct[-ual][+ed] value
```

### Generated Code Validation

**Input:**
```csharp
Assert(await GetBoolAsync());
```

**Output:**
```csharp
#line 36 "/Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/09_AsyncDemos.cs"
await global::SharpAssert.SharpInternal.AssertAsync(
    async()=>await GetBoolAsync(),
    "await GetBoolAsync()",
    "/Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/09_AsyncDemos.cs",
    36)
#line default
;
```

✓ Properly awaited
✓ Correct spacing
✓ Line directives in correct position
✓ Async lambda wraps await expression

## Lessons Learned

### 1. Test Coverage Gaps

The existing test that claimed to verify async handling was actually verifying the WRONG behavior (skipping rewrite instead of performing it). This masked the bug entirely.

**Action**: Review all rewriter tests for correctness, not just pass/fail.

### 2. Integration Testing Critical

Unit tests passed, but integration tests would have caught this immediately if they existed for async binary comparisons.

**Action**: Add comprehensive integration tests for all rewrite scenarios.

### 3. Demo Project as Living Documentation

The demo project immediately exposed the bug when properly configured. It serves as:
- Integration validation
- Living documentation
- User experience validation

**Action**: Always run demo project after major changes.

### 4. Roslyn Syntax Tree Complexity

Creating proper `AwaitExpression` nodes with correct trivia (spacing, line directives) requires careful attention to:
- Token construction with leading/trailing trivia
- Node-level trivia attachment
- Preprocessor directive placement rules

**Action**: Document Roslyn trivia patterns in learnings.md.

### 5. Return Type Changes Ripple

Changing `RewriteToAsync()` return type from `InvocationExpressionSyntax` to `AwaitExpressionSyntax` required:
- New helper method `AddLineDirectivesToAwait()`
- Different trivia attachment strategy
- Explicit await token construction

**Action**: Consider visitor pattern refactoring to reduce coupling.

## Related Files

### Modified
- `src/SharpAssert/SharpAssertRewriter.cs` - Core fix
- `src/SharpAssert.Demo/SharpAssert.Demo.csproj` - MSBuild integration
- `src/SharpAssert.IntegrationTests/AsyncIntegrationFixture.cs` - Test updates

### Needs Updating
- `src/SharpAssert.Tests/Rewriter/RewriterFixture.cs:93-114` - Fix incorrect test
- `learnings.md` - Add Roslyn trivia patterns
- `README.md` - Document async assertion syntax

## Implementation Checklist for Future

If this needs to be re-implemented or ported:

- [ ] Implement async detection (`ContainsAwait()`)
- [ ] Implement dynamic detection (`ContainsDynamic()`)
- [ ] Add rewrite routing logic with priority: async > dynamic > sync
- [ ] Implement `RewriteToAsync()` returning `AwaitExpressionSyntax`
- [ ] Implement `RewriteToAsyncBinary()` returning `AwaitExpressionSyntax`
- [ ] Implement `RewriteToDynamic()` returning `InvocationExpressionSyntax`
- [ ] Implement `RewriteToDynamicBinary()` returning `InvocationExpressionSyntax`
- [ ] Create `CreateAsyncLambda()` helper
- [ ] Create `CreateAsyncThunk()` with proper await detection
- [ ] Create `CreateDynamicThunk()` with object? cast
- [ ] Create `WrapInTaskFromResult()` for sync operands
- [ ] Add explicit spacing after await keyword (trivia list with space)
- [ ] Create `AddLineDirectivesToAwait()` for proper directive placement
- [ ] Update `CreateAsyncInvocation()` method signature
- [ ] Update `CreateAsyncBinaryInvocation()` method signature
- [ ] Update `CreateDynamicInvocation()` method signature
- [ ] Update `CreateDynamicBinaryInvocation()` method signature
- [ ] Add unit tests for each rewrite scenario
- [ ] Add integration tests for async binary comparisons
- [ ] Add integration tests for mixed async/sync
- [ ] Add integration tests for async string diffs
- [ ] Add rewriter output validation tests
- [ ] Add dynamic detection tests
- [ ] Fix incorrect "skip async" test
- [ ] Run demo project to validate all scenarios
- [ ] Update learnings.md with Roslyn trivia patterns

## References

- **PRD Section 4.2**: Async/Dynamic priority rules
- **Increment 10**: Basic async support
- **Increment 11**: Async binary comparisons
- **Increment 12**: Dynamic support
- **learnings.md Lines 197-228**: Async/Dynamic implementation notes
