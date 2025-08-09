# PRD: Sharp Assertions — Hybrid “Lambda‑Rewrite + Runtime” (PowerAssert‑style) for .NET

**Status:** Final, implementation‑ready  
**Scope:** Test projects (xUnit/NUnit/MSTest), .NET 6+  
**License:** MIT

**Goal:** Pytest‑style assertions with one public API call (`Sharp.Assert(...)`) and automatic MSBuild source‑rewrite to a lambda form that enables rich, PowerAssert‑like diagnostics at runtime.

**Why hybrid?** We keep the rewriter very small (wraps into a lambda) and move the heavy lifting to runtime (expression‑tree analysis). We add targeted fallbacks for `await` and `dynamic`.

---

## 1. High‑Level Design

### 1.1 What users write

```csharp
using static Sharp;

Assert(result == expected);
Assert(isAuth && hasPerms);
Assert(items.Contains(item));
Assert(actual.SequenceEqual(expected));
```

### 1.2 What the MSBuild rewriter emits (simplified)

- **Sync, no `await`, no `dynamic`:**
  ```csharp
  SharpInternal.Assert(() => result == expected, expr: "result == expected", file: "...", line: 42);
  ```

- **Async binary with `await`:**
  ```csharp
  SharpInternal.AssertAsyncBinary(
      left:  async () => await client.GetAsync(),
      right: async () => await svc.GetAsync(),
      op: BinaryOp.Eq,
      expr: "await client.GetAsync() == await svc.GetAsync()", file: "...", line: 10);
  ```

- **Async (general `await` in condition):**
  ```csharp
  SharpInternal.AssertAsync(async () => await CheckAsync(a,b), expr: "...", file: "...", line: 12);
  ```

- **Dynamic binary:**
  ```csharp
  SharpInternal.AssertDynamicBinary(
      left: () => (object?)x.Some(),
      right: () => (object?)y,
      op: BinaryOp.Gt,
      expr: "x.Some() > y", file: "...", line: 27);
  ```

- **Dynamic (general):**
  ```csharp
  SharpInternal.AssertDynamic(() => x.SomeDynamicCall() > 10, expr: "...", file: "...", line: 30);
  ```

Users see and call only `Sharp.Assert(bool)`. Everything else lives in `SharpInternal` and is called by the rewriter.

---

## 2. Public & Internal APIs

### 2.1 Public surface (what users see)

```csharp
public static class Sharp
{
    /// Entry point users call in tests. Rewriter replaces this call.
    public static void Assert(
        bool condition,
        [System.Runtime.CompilerServices.CallerArgumentExpression("condition")] string? expr = null,
        [System.Runtime.CompilerServices.CallerFilePath] string? file = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int line = 0);
}
```

### 2.2 Internal runtime APIs (what the rewriter targets)

```csharp
public static class SharpInternal
{
    // SYNC: expression tree (PowerAssert-style)
    public static void Assert(
        System.Linq.Expressions.Expression<Func<bool>> condition,
        string expr, string file, int line);

    // ASYNC (general): no expression tree (await is not allowed in Expression<T>)
    public static Task AssertAsync(
        Func<Task<bool>> conditionAsync,
        string expr, string file, int line);

    // ASYNC (binary) – to get left/right values for diffs
    public static Task AssertAsyncBinary(
        Func<Task<object?>> leftAsync,
        Func<Task<object?>> rightAsync,
        BinaryOp op,
        string expr, string file, int line);

    // DYNAMIC (general): run once, limited diagnostics
    public static void AssertDynamic(
        Func<bool> condition,
        string expr, string file, int line);

    // DYNAMIC (binary): capture left/right via thunks, compare via dynamic binder
    public static void AssertDynamicBinary(
        Func<object?> left,
        Func<object?> right,
        BinaryOp op,
        string expr, string file, int line);
}

public enum BinaryOp  { Eq, Ne, Lt, Le, Gt, Ge }
public enum LogicalOp { And, Or }
```

**Rationale:**
- `Expression<Func<bool>>` unlocks rich sync diagnostics.
- `await`/`dynamic` cannot be inside expression trees → we use thunks (`Func<Task<object?>>` / `Func<object?>`) and do the minimal yet useful diagnostics.

---

## 3. Runtime Behavior (Diagnostics)

### 3.1 Expression‑tree (sync) path: `SharpInternal.Assert(Expression<Func<bool>>)`
- Walk the expression tree and evaluate sub‑expressions once (cache results).
- **Supported nodes & behaviors (PowerAssert‑style):**
    - **Binary compares** `==` `!=` `<` `<=` `>` `>=` → print left/right values; type‑aware comparisons.
    - **Logical** `&&` `||` `!` → print operand truth values (preserve short‑circuit semantics naturally).
    - **Membership/LINQ:** detect and enhance:
        - `.Contains(item)` → show item, `Count`, preview of collection (first N), result.
        - `.Any(pred)` / `.All(pred)` → show predicate, `Count`, subset of matching/failing elements (truncated).
        - `.SequenceEqual(other)` → produce diff (see 3.4).
    - **Calls, indexers, member access** that feed into the boolean → capture values as needed for a clear message.
    - **Strings vs strings:** produce inline (default) or side‑by‑side diff via DiffPlex.
    - **Collections vs collections:** produce readable mismatch reports (index of first diff, missing/extra) using FluentAssertions or an internal LCS; include previews.
    - **Objects/records/structs:** when equality fails, run a deep diff via Compare‑Net‑Objects (path‑level differences).
- On failure, throw `SharpAssertionException` with a well‑formatted message (includes expression text, file, line).

### 3.2 Async (general): `AssertAsync(Func<Task<bool>>)`
- Evaluate once, `await` result.
- Failure message includes `expr`, file/line, and boolean `False`.
- (Optional later) If rewriter attached light metadata, we can print a hint (e.g., “logical expression with await — limited diagnostics”).

### 3.3 Async (binary): `AssertAsyncBinary(Func<Task<object?>> left, Func<Task<object?>> right, BinaryOp op, …)`
- `await` left then right (in the source order) exactly once each.
- Compute comparison and, on failure, render like sync binary:
    - **strings** → DiffPlex
    - **collections** → FA diff
    - **objects** → Compare‑Net‑Objects
    - **primitives** → print values

### 3.4 `SequenceEqual` rich diff (sync path; async via binary thunks)
- Materialize both sequences to `List<T>` once each (avoid re‑enumeration).
- Show per‑index comparisons (mismatch markers) and a compact unified diff (DiffPlex).
- Truncate large outputs; show first/last N with `…`.

### 3.5 Dynamic (general/binary)
- `AssertDynamic(Func<bool>)` → evaluate once, print minimal report (`expr`, `False`).
- `AssertDynamicBinary(Func<object?> left, Func<object?> right, op, …)`:
    - Evaluate left/right once; compute `(dynamic)left OP (dynamic)right`; if false → print values; apply string/collection/object diff rules where appropriate.

### 3.6 External diff viewers (optional)
- Integrate `Verify.*`: when string/object diffs are large, write `.received`/`.verified` files and open configured diff tool (VS Code, Beyond Compare, etc.).
- For NUnit/MSTest: attach files via `TestContext.AddTestAttachment` / `AddResultFile`.

### 3.7 Performance Considerations
- The rich diagnostic tools (object diffing, collection comparison) are only invoked on assertion failure. While this is efficient for passing tests, be aware that complex object diffs on large structures can introduce a performance cost *at the moment of failure*. This is generally an acceptable trade-off for the detailed feedback provided.

---

## 4. MSBuild Source Rewrite Task

### 4.1 Build integration

```xml
<PropertyGroup>
  <EnableSharpLambdaRewrite>true</EnableSharpLambdaRewrite>
</PropertyGroup>

<Target Name="SharpLambdaRewrite" BeforeTargets="CoreCompile"
        Condition="'$(EnableSharpLambdaRewrite)'=='true'">
  <ItemGroup>
    <_SharpInput Include="@(Compile)" />
  </ItemGroup>

  <SharpLambdaRewriteTask
      Sources="@(_SharpInput)"
      ProjectDir="$(MSBuildProjectDirectory)"
      IntermediateDir="$(IntermediateOutputPath)"
      OutputDir="$(IntermediateOutputPath)SharpRewritten"
      LangVersion="$(LangVersion)"
      NullableContext="$(Nullable)" />

  <ItemGroup>
    <Compile Remove="@(Compile)" />
    <Compile Include="$(IntermediateOutputPath)SharpRewritten\**\*.sharp.g.cs" />
  </ItemGroup>
</Target>
```

### 4.2 Rewriter algorithm (deterministic)

**Core Principle:** The rewriter must be robust. If it fails to analyze an assertion for any reason, it **must** silently leave the original `Sharp.Assert` call untouched. This provides a graceful fallback to the default `[CallerArgumentExpression]` behavior, preventing build failures from complex or unexpected user code.

For each `InvocationExpression` resolving to `Sharp.Assert(bool)`:
1.  **Analyze the argument with `SemanticModel`:**
    - Contains `await`? → `HasAwait = true`
    - Contains `dynamic` ops? → `HasDynamic = true`
    - Top‑level binary operator? (`Eq`/`Ne`/`Lt`/`Le`/`Gt`/`Ge`) → `BinaryOp?`
2.  **Rewrite decision matrix**

| Case                 | Rewrite to                                                                                             | Notes                                                                                              |
| -------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------- |
| No `await`, no `dynamic` | `SharpInternal.Assert(() => <expr>, expr, file, line)`                                                 | Main sync path (expression tree)                                                                   |
| `await` + binary       | `SharpInternal.AssertAsyncBinary(async () => <left>, async () => <right>, op, expr, file, line)`         | Left/right are awaitable. If one side is sync, it's wrapped in `Task.FromResult()` to match the signature. |
| `await` + not binary   | `SharpInternal.AssertAsync(async () => <expr>, expr, file, line)`                                      | Limited diagnostics                                                                                |
| `dynamic` + binary     | `SharpInternal.AssertDynamicBinary(() => (object?)<left>, () => (object?)<right>, op, expr, file, line)` | Better diagnostics with values                                                                     |
| `dynamic` + not binary | `SharpInternal.AssertDynamic(() => <expr>, expr, file, line)`                                          | Minimal diagnostics                                                                                |
| Both `await` & `dynamic` | Prefer async track; if binary → `AssertAsyncBinary` using thunks that include dynamic parts.           |                                                                                                    |

3.  **Emit fully‑qualified `global::SharpInternal.*` calls.**
    (No `#line` needed — we aren’t expanding large blocks. Debugging stays natural.)

**Note:** You can include the raw `expr` string using `[CallerArgumentExpression]` from the original `Sharp.Assert`, or the rewriter can embed it literally. Prefer embedding the literal for clarity.

### 4.3 Debugging the Rewriter
To facilitate debugging, the rewrite task will support a diagnostic MSBuild property:
```xml
<PropertyGroup>
  <SharpAssertEmitRewriteInfo>true</SharpAssertEmitRewriteInfo>
</PropertyGroup>
```
When enabled, the rewriter will output detailed logs of its analysis and decisions. This is crucial for troubleshooting unexpected rewrite behavior.

---

## 5. Output Formatting & Limits
- **Header:** `Assertion failed: <expr>  at <file>:<line>`
- **Binary:** print semantic operator and both values.
- **Strings:** DiffPlex inline (default). Configurable side‑by‑side and context lines.
- **Collections:** first mismatch index; missing/extra; preview first/last N; avoid gigantic dumps.
- **Objects:** list up to M property‑level diffs (path → left vs right).
- **Truncation:** apply global limits; append `…` (truncated).

---

## 6. Configuration (Thread-Safe)

Configuration is handled via an `AsyncLocal<T>`-based context, ensuring that settings are safely applied even when tests run in parallel. A global default is available, but can be overridden for a specific scope using a `using` block.

```csharp
public sealed class SharpOptions
{
    public int  MaxDiffLines { get; init; } = 200;
    public int  MaxCollectionPreview { get; init; } = 50;
    public CollectionEqualityMode Collections { get; init; } = CollectionEqualityMode.Strict; // or Equivalent
    public bool StringsSideBySide { get; init; } = false;
    public bool UseVerify { get; init; } = false;
    public bool OpenExternalDiff { get; init; } = false;
    public string? VerifyDirectory { get; init; } = null;
}

public static class SharpConfig
{
    private static readonly AsyncLocal<SharpOptions?> _options = new();

    public static SharpOptions Current => _options.Value ??= new SharpOptions();

    public static IDisposable WithOptions(SharpOptions options)
    {
        var original = _options.Value;
        _options.Value = options;
        return new ScopedOptions(() => _options.Value = original);
    }

    private sealed class ScopedOptions : IDisposable { /* ... */ }
}

public enum CollectionEqualityMode { Strict, Equivalent }
```

**Example Usage:**
```csharp
// This assertion uses the default/global options
Assert(myString == "expected");

// Temporarily override settings for a specific block of tests
using (SharpConfig.WithOptions(new SharpOptions { StringsSideBySide = true }))
{
    Assert(myString == "a different string"); // This assertion will use side-by-side diffs
}
```

---

## 7. Dependencies
- **DiffPlex** — string & sequence diffs
  ```bash
  dotnet add package DiffPlex
  ```
- **FluentAssertions** — readable collection diffs & predicate messaging
  ```bash
  dotnet add package FluentAssertions
  ```
- **Compare‑Net‑Objects** — deep object diffs
  ```bash
  dotnet add package KellermanSoftware.CompareNetObjects
  ```
- **Verify.*** (optional) — external diff tooling
  ```bash
  dotnet add package Verify.Xunit # (or NUnit/MSTest variants)
  ```

---

## 8. Implementation Plan

### **Increment 1: Foundation - Basic Assert with Exception** ✅ COMPLETED
**Outcome**: Users can call `Sharp.Assert(bool)` and get meaningful failures
**Tests** (SharpAssert.Tests/AssertionFixture.cs):
- ✅ `Should_pass_when_condition_is_true()` - Assert(true) doesn't throw
- ✅ `Should_throw_SharpAssertionException_when_false()` - Assert(false) throws with message
- ✅ `Should_include_expression_text_in_error()` - Assert(1==2) shows "1==2" via CallerArgumentExpression
- ✅ `Should_include_file_and_line_in_error()` - Error contains file path and line number

**Implementation**:
- ✅ Create `Sharp.cs` with public static `Assert(bool, CallerArgumentExpression, CallerFilePath, CallerLineNumber)`
- ✅ Create `SharpAssertionException : Exception` with formatted message
- ✅ Assert throws exception when condition is false with expression/file/line info

---

### **Increment 2: Expression Tree Runtime - Binary Comparisons**
**Outcome**: Runtime can analyze binary expressions and show operand values
**Tests** (SharpAssert.Tests/ExpressionAnalysisFixture.cs):
- `Should_show_left_and_right_values_for_equality()` - x==y shows both values
- `Should_handle_all_comparison_operators()` - Test ==, !=, <, <=, >, >=
- `Should_handle_null_operands()` - null == value shows "null" properly
- `Should_evaluate_complex_expressions_once()` - Side effects happen only once

**Implementation**:
- Create `SharpInternal.cs` with `Assert(Expression<Func<bool>>, string, string, int)`
- Implement `ExpressionVisitor` that walks tree and caches evaluated sub-expressions
- Format binary operators with left/right values in error message
- Create `BinaryOp` enum

---

### **Increment 3: Logical Operators Support**
**Outcome**: Logical operators (&&, ||, !) show operand truth values
**Tests** (SharpAssert.Tests/LogicalOperatorFixture.cs):
- `Should_show_which_part_of_AND_failed()` - true && false shows right was false  
- `Should_short_circuit_AND_correctly()` - false && throw doesn't evaluate right
- `Should_show_which_part_of_OR_succeeded()` - false || true shows evaluation
- `Should_handle_NOT_operator()` - !true shows operand was true

**Implementation**:
- Extend ExpressionVisitor for AndAlso, OrElse, Not nodes
- Preserve short-circuit semantics naturally via expression evaluation
- Format logical operations clearly in error messages

---

### **Increment 4: MSBuild Rewriter - Sync Cases Only**
**Outcome**: Build rewrites `Assert(expr)` to `SharpInternal.Assert(() => expr, ...)`
**Tests** (SharpAssert.Rewriter.Tests/RewriterFixture.cs):
- `Should_rewrite_simple_assertion_to_lambda()` - Assert(x==1) becomes lambda
- `Should_preserve_complex_expressions()` - Nested calls preserved
- `Should_skip_rewrite_if_async_present()` - Detects await, leaves original
- `Should_handle_multiple_assertions_in_file()` - All assertions rewritten

**Implementation**:
- Create MSBuild task project SharpAssert.Rewriter
- Use Roslyn to parse, analyze with SemanticModel, detect Sharp.Assert calls
- Generate lambda wrapping for sync cases
- Write to intermediate directory
- Create .targets file for integration

---

### **Increment 5: String Comparison with Inline Diffs**
**Outcome**: String equality failures show character-level differences
**Tests** (SharpAssert.Tests/StringComparisonFixture.cs):
- `Should_show_inline_diff_for_strings()` - "hello" vs "hallo" shows diff
- `Should_handle_multiline_strings()` - Line-by-line comparison
- `Should_truncate_very_long_strings()` - Limits output size
- `Should_handle_null_strings()` - null vs "" handled gracefully

**Implementation**:
- Add DiffPlex NuGet package
- Detect string comparisons in ExpressionVisitor
- Create StringDiffer class using DiffPlex inline diff builder
- Apply truncation limits from configuration

---

### **Increment 6: Collection Comparison - Basic**
**Outcome**: Collection failures show first mismatch and missing/extra elements
**Tests** (SharpAssert.Tests/CollectionComparisonFixture.cs):
- `Should_show_first_mismatch_index()` - [1,2,3] vs [1,2,4] shows index 2
- `Should_show_missing_elements()` - [1,2] vs [1,2,3] shows missing 3
- `Should_show_extra_elements()` - [1,2,3] vs [1,2] shows extra 3
- `Should_handle_empty_collections()` - [] vs [1] handled correctly

**Implementation**:
- Detect IEnumerable comparisons
- Materialize to List<T> once to avoid re-enumeration
- Calculate first difference, missing, extra
- Format collection preview (first/last N elements)

---

### **Increment 7: Object Deep Comparison**
**Outcome**: Object/record/struct failures show property-level differences
**Tests** (SharpAssert.Tests/ObjectComparisonFixture.cs):
- `Should_show_property_differences()` - Different property values listed
- `Should_handle_nested_objects()` - Deep path shown (e.g., "Address.City")
- `Should_handle_null_objects()` - null vs instance handled
- `Should_respect_equality_overrides()` - Uses Equals if overridden

**Implementation**:
- Add Compare-Net-Objects NuGet package
- Detect object equality comparisons
- Use CompareLogic to get differences
- Format property paths with old vs new values
- Apply MaxDiffLines limit

---

### **Increment 8: LINQ Operations - Contains/Any/All**
**Outcome**: LINQ operations provide specialized diagnostic messages
**Tests** (SharpAssert.Tests/LinqOperationsFixture.cs):
- `Should_show_collection_when_Contains_fails()` - Shows actual collection contents
- `Should_show_matching_items_for_Any()` - Shows which items matched predicate
- `Should_show_failing_items_for_All()` - Shows which items failed predicate
- `Should_handle_empty_collections_in_LINQ()` - Empty.Any() shows "empty collection"

**Implementation**:
- Detect MethodCallExpression for Contains, Any, All
- Materialize collection once, apply predicate
- Show collection count, preview of elements
- Format predicate expression if available

---

### **Increment 9: SequenceEqual Deep Diff**
**Outcome**: SequenceEqual shows unified diff of sequences
**Tests** (SharpAssert.Tests/SequenceEqualFixture.cs):
- `Should_show_unified_diff()` - Side-by-side comparison
- `Should_handle_different_lengths()` - Shows length mismatch
- `Should_truncate_large_sequences()` - Limits output with "..."
- `Should_work_with_custom_comparers()` - Honors IEqualityComparer parameter

**Implementation**:
- Detect SequenceEqual method call
- Materialize both sequences to List<T>
- Use DiffPlex for unified diff
- Apply truncation for large outputs

---

### **Increment 10: Async Support - Basic AssertAsync**
**Outcome**: Can assert on expressions containing await
**Tests** (SharpAssert.Tests/AsyncAssertionFixture.cs):
- `Should_handle_await_in_condition()` - Assert(await GetBool()) works
- `Should_show_false_for_failed_async()` - Shows expression and False
- `Should_preserve_async_context()` - Maintains SynchronizationContext
- `Should_handle_exceptions_in_async()` - Async exceptions bubble correctly

**Implementation**:
- Create `SharpInternal.AssertAsync(Func<Task<bool>>, ...)`
- Rewriter detects await keyword via SemanticModel
- Emits AssertAsync for general await cases
- Await result and provide basic diagnostics

---

### **Increment 11: Async Binary Comparisons**
**Outcome**: Binary comparisons with await show both operand values
**Tests** (SharpAssert.Tests/AsyncBinaryFixture.cs):
- `Should_show_both_async_values()` - await Left() == await Right() shows values
- `Should_handle_mixed_async_sync()` - await X() == 5 works correctly
- `Should_evaluate_in_source_order()` - Left evaluated before right
- `Should_apply_diffs_to_async_strings()` - String diff works with async

**Implementation**:
- Create `SharpInternal.AssertAsyncBinary(Func<Task<object?>>, Func<Task<object?>>, BinaryOp, ...)`
- Rewriter detects binary with await, generates thunks
- Wrap sync operands in Task.FromResult
- Apply same diff logic as sync path

---

### **Increment 12: Dynamic Support**
**Outcome**: Dynamic expressions work with value diagnostics for binaries
**Tests** (SharpAssert.Tests/DynamicAssertionFixture.cs):
- `Should_handle_dynamic_binary()` - dynamic == 5 shows values
- `Should_handle_dynamic_method_calls()` - dynamic.Method() > 0 works
- `Should_apply_dynamic_operator_semantics()` - Uses DLR for comparison
- `Should_show_minimal_diagnostics_for_complex_dynamic()` - Falls back gracefully

**Implementation**:
- Create `SharpInternal.AssertDynamic` and `AssertDynamicBinary`
- Rewriter detects dynamic via SemanticModel
- Use dynamic binder for operator evaluation
- Cast to object? in thunks for binary cases

---

### **Increment 13: Thread-Safe Configuration**
**Outcome**: Tests can override configuration without affecting parallel tests
**Tests** (SharpAssert.Tests/ConfigurationFixture.cs):
- `Should_use_default_options()` - Default values applied
- `Should_override_with_scoped_options()` - using block changes settings
- `Should_restore_after_scope()` - Original settings restored
- `Should_isolate_parallel_test_configs()` - Parallel tests don't interfere

**Implementation**:
- Create SharpOptions record with settings
- Implement SharpConfig with AsyncLocal<SharpOptions>
- WithOptions returns IDisposable for scoping
- All formatters read from SharpConfig.Current

---

### **Increment 14: Rewriter Robustness & Fallback**
**Outcome**: Complex/invalid expressions gracefully fall back to original Assert
**Tests** (SharpAssert.Rewriter.Tests/FallbackFixture.cs):
- `Should_leave_invalid_syntax_unchanged()` - Malformed code not rewritten
- `Should_handle_compilation_errors_gracefully()` - Doesn't crash build
- `Should_emit_diagnostic_logs_when_enabled()` - SharpAssertEmitRewriteInfo works
- `Should_handle_exotic_expression_types()` - Patterns, switch expressions, etc.

**Implementation**:
- Wrap rewriter logic in try-catch
- If analysis fails, leave original Assert call
- Add diagnostic logging controlled by MSBuild property
- Test with edge cases and invalid code

---

### **Increment 15: Integration & Polish**
**Outcome**: Complete integration with test frameworks and external tools
**Tests** (SharpAssert.Integration.Tests/*):
- `Should_work_with_xunit()` - Full xUnit integration test
- `Should_work_with_nunit()` - Full NUnit integration test  
- `Should_attach_files_to_test_context()` - NUnit/MSTest file attachments
- `Should_respect_all_config_options()` - Verify all settings work

**Implementation**:
- Create example projects for each test framework
- Add Verify.* integration (optional)
- Polish error message formatting
- Create NuGet package with .targets file
- Documentation and samples

---

### Verification Steps for Each Increment

Each increment must:
1. ✅ Have all tests written first and failing appropriately
2. ✅ Implement minimal code to make tests pass
3. ✅ Achieve 100% branch coverage for new code
4. ✅ Run `dotnet build` successfully
5. ✅ Pass all existing tests (no regressions)
6. ✅ Update learnings.md with any discoveries
7. ✅ Commit with descriptive message

---

## 9. Testing Strategy
- **Runtime unit tests (expression‑tree):**
    - Binaries, logicals, membership, `SequenceEqual`, strings, collections, objects, nulls, type mismatches.
- **Async tests:**
    - `AssertAsyncBinary` with awaited thunks (order & single evaluation).
    - `AssertAsync` minimal diagnostics.
- **Dynamic tests:**
    - `AssertDynamicBinary` for common ops; collection/object/string values printed correctly.
- **Rewriter tests:**
    - Source → rewritten source (golden files) for each mapping row; ensure no `await` or `dynamic` leaks into `Expression<Func<bool>>`.
    - Test the graceful fallback mechanism where the rewriter leaves invalid code alone.
- **Integration:**
    - Real test project with `EnableSharpLambdaRewrite=true`; run & verify messages.
    - Large collections/strings ensure truncation works.
    - Test configuration scoping with `SharpConfig.WithOptions`.

---

## 10. Mapping Table (rewrite rules)

| Case                 | User Code                           | Emits                                                                                             |
| -------------------- | ----------------------------------- | ------------------------------------------------------------------------------------------------- |
| Sync, normal         | `Assert(a + 2 == b * 3)`            | `SharpInternal.Assert(() => a + 2 == b * 3, "a + 2 == b * 3", file, line)`                        |
| Sync, logical        | `Assert(isAuth && hasPerms)`        | `SharpInternal.Assert(() => isAuth && hasPerms, "...", file, line)`                                |
| `.Contains`          | `Assert(users.Contains(admin))`     | `SharpInternal.Assert(() => users.Contains(admin), "...", file, line)`                            |
| `.Any` / `.All`      | `Assert(items.Any(p))`              | `SharpInternal.Assert(() => items.Any(p), "...", file, line)`                                     |
| `.SequenceEqual`     | `Assert(a.SequenceEqual(b))`        | `SharpInternal.Assert(() => a.SequenceEqual(b), "...", file, line)`                               |
| Async binary         | `Assert(await L() == await R())`    | `SharpInternal.AssertAsyncBinary(async () => await L(), async () => await R(), Eq, "...", file, line)` |
| Mixed async/sync     | `Assert(await L() == "constant")`   | `SharpInternal.AssertAsyncBinary(async () => await L(), () => Task.FromResult("constant"), ...)`   |
| Async general        | `Assert(await CheckAsync(x))`       | `SharpInternal.AssertAsync(async () => await CheckAsync(x), "...", file, line)`                   |
| Dynamic binary       | `Assert(xDyn == y)`                 | `SharpInternal.AssertDynamicBinary(() => (object?)xDyn, () => (object?)y, Eq, "...", file, line)`  |
| Dynamic general      | `Assert(xDyn.Call() > 0)`           | `SharpInternal.AssertDynamic(() => xDyn.Call() > 0, "...", file, line)`                           |
| Both `await` & `dynamic` | `Assert(await xDyn.F() == y)`       | Prefer async binary thunks that return `object?`                                                  |

---

## 11. Non‑Goals (initial)
- Full AST explanation for `async` and `dynamic` (beyond the targeted thunks).
- Expression‑trees for `await` / `dynamic` (not supported by C#).
- Rewriting outside test projects (default opt‑in per test `csproj`).

---

## 12. Acceptance Criteria
- Devs write only `Sharp.Assert(bool)`; rewriter does the rest.
- **Robustness:** The build does not fail due to rewriter errors; it gracefully falls back to simpler diagnostics.
- Sync cases yield PowerAssert‑grade messages: left/right, logical operand values, membership/LINQ insights, `SequenceEqual` diff, string/collection/object diffs.
- Async/dynamic covered with targeted thunks; at minimum, clear failure text; for binaries — proper value diffs.
- No double evaluation; order preserved.
- Easy to read messages, truncated sensibly; optional external diff viewer.
- Configuration is thread-safe and easy to use.

---

## 13. Nice‑to‑Have (future)
- Per‑call options (`Assert(…, new SharpOptions { … })`).
- Custom printers/formatters registry for domain types.
- HTML diff emitter (for CI artifacts).
- SourceLink‑based embedding of original `expr` text (if needed).

---

This PRD gives a clear, minimal rewrite strategy (wrap to lambda / emit async/dynamic thunks) and a rich runtime that mirrors pytest’s UX, including great diffs for strings, collections, sequences, and deep object graphs. The walking skeleton can be implemented fast, then iterated safely.