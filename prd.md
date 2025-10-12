# SharpAssert PRD

**Status:** Active Development
**Scope:** Test projects (xUnit/NUnit/MSTest), .NET 9+
**License:** MIT

## Foundational Principles

**See [CONSTITUTION.md](CONSTITUTION.md)** for the immutable architectural principles and core philosophy.

Key invariants:
- Hybrid architecture: minimal rewriter, rich runtime
- Never break the build (graceful fallback)
- Single evaluation, single materialization
- Simple API, rich diagnostics
- Pay-on-failure performance

---

## Implementation Roadmap

### High Priority (Polish & Correctness)

#### 1. Move All Tests to Integration Tests (Front Door Only)
**Goal:** Test only via public API `Assert()`, not internal methods.

**Why:** Ensures we're testing user-facing behavior, catches integration issues.

**Tasks:**
- Move unit tests from `SharpInternal.*` direct calls to `Sharp.Assert()` calls
- Keep tests focused on behavior, not implementation
- Verify all edge cases still covered

---

#### 2. Enhanced Dynamic Support (Objects/Collections/ExpandoObject)
**Goal:** Make dynamic object comparisons work with proper formatting.

**Current State:** Basic dynamic binary/general implemented but needs extension.

**Test Cases:**
```csharp
// JSON deserialization
dynamic json = JsonSerializer.Deserialize<dynamic>(jsonString);
Assert(json.user.name == "expected");

// ExpandoObject
dynamic person = new ExpandoObject();
person.Name = "Yevhen";
person.Age = 42;
Assert(person.Age > 40);

// Dynamic collections
dynamic obj = new { Items = new[] { 1, 2, 3 } };
Assert(obj.Items.Contains(5));  // Should show collection contents
```

**Tasks:**
- Test dynamic objects with nested properties
- Test dynamic with collections (arrays, lists)
- Test ExpandoObject scenarios
- Ensure proper formatting for dynamic objects in diagnostics

---

#### 3. Rewriter Diagnostic Logging (`SharpAssertEmitRewriteInfo`)
**Goal:** Support MSBuild property for troubleshooting rewriter behavior.

**Implementation:**
```xml
<PropertyGroup>
  <SharpAssertEmitRewriteInfo>true</SharpAssertEmitRewriteInfo>
</PropertyGroup>
```

**Tasks:**
- Add logging infrastructure to rewriter task
- Log: detected await/dynamic, chosen rewrite path, fallback reasons
- Write logs to MSBuild diagnostic output
- Document in README/troubleshooting guide

---

#### 4. Documentation: Dependency Attribution
**Goal:** Properly credit all third-party dependencies in README.md.

**Dependencies to attribute:**
- **DiffPlex** — string & sequence diffs
- **Compare-Net-Objects** — deep object diffs
- **PowerAssert** — optional alternative/fallback mode (clarify current usage)

**Tasks:**
- Add "Credits" or "Dependencies" section to README.md
- Link to each package's repository
- Clarify PowerAssert relationship (optional vs. fallback vs. not used)
- Review all references to PowerAssert fallback behavior

---

### Medium Priority (New Features)

#### 5. External Diff Viewers (Verify.* Integration)
**Goal:** For large diffs, write `.received`/`.verified` files and open configured diff tool.

**Research Needed:**
- How PyCharm/pytest shows inline diffs in IDE
- What triggers "large diff" threshold
- Best UI/UX for .NET developers

**Implementation:**
- Integrate Verify.* packages (Verify.Xunit, Verify.NUnit, Verify.MSTest)
- Write assertion failure artifacts to `.received`/`.verified` files
- Open configured diff tool (VS Code, Beyond Compare, etc.)
- For NUnit/MSTest: attach files via `TestContext.AddTestAttachment` / `AddResultFile`

**Dependencies:**
```bash
dotnet add package Verify.Xunit  # or NUnit/MSTest variants
```

---

#### 6. Expose Direct PowerAssert Integration
**Goal:** Allow users to call PowerAssert directly via `Sharp.Assert(() => ...)` overload.

**API:**
```csharp
// Current: Sharp.Assert(bool condition)
// New: Sharp.Assert(Expression<Func<bool>> condition)  // delegates to PowerAssert
```

**Why:** Gives users escape hatch if they prefer PowerAssert diagnostics for specific assertions.

**Tasks:**
- Add overload that accepts `Expression<Func<bool>>`
- Delegate directly to PowerAssert.PAssert.IsTrue
- Document when to use this overload
- Test that it works alongside rewritten assertions

---

#### 7. Custom Formatters Registry
**Goal:** Allow users to register custom formatters for domain types.

**API:**
```csharp
SharpConfig.RegisterFormatter<MyType>((value, context) =>
{
    return $"MyType({value.Id}, {value.Name})";
});
```

**Tasks:**
- Design formatter API (signature, context object)
- Implement formatter registry (thread-safe)
- Integrate into diagnostic pipeline
- Document with examples

---

#### 8. Custom Comparers
**Goal:** Allow users to register custom equality comparers for types.

**API:**
```csharp
SharpConfig.RegisterComparer<MyType>((left, right) =>
{
    return left.Id == right.Id;  // Custom equality logic
});
```

**Tasks:**
- Design comparer API
- Implement comparer registry (thread-safe)
- Integrate into comparison logic
- Document with examples

---

### Low Priority (Nice-to-Have)

#### 9. HTML Diff Emitter
**Goal:** Generate HTML diff artifacts for CI/CD pipelines.

**Implementation:**
- Render assertion failures as formatted HTML
- Include syntax highlighting, collapsible sections
- Write to artifacts directory for CI systems

---

#### 10. SourceLink-based Expression Embedding
**Goal:** Use SourceLink to embed original expression text without CallerArgumentExpression.

**Rationale:**
- More reliable than CallerArgumentExpression in edge cases
- Works across compilation boundaries

---

## Reference Documentation

### Current Implementation Status

All core features are **✅ COMPLETED**:
- Basic assertions with CallerArgumentExpression
- Expression tree runtime with binary/logical operators
- MSBuild rewriter with PowerAssert fallback
- String diffs (DiffPlex)
- Collection comparisons
- Object deep diffs (Compare-Net-Objects)
- LINQ operations (Contains/Any/All/SequenceEqual)
- Async/await support
- Dynamic type support (basic)
- Nullable type support
- Graceful rewriter fallback

### API Reference

**Public API:**
```csharp
public static class Sharp
{
    public static void Assert(bool condition,
        [CallerArgumentExpression("condition")] string? expr = null,
        [CallerFilePath] string? file = null,
        [CallerLineNumber] int line = 0);

    public static ExceptionResult<T> Throws<T>(Action action) where T : Exception;
    public static Task<ExceptionResult<T>> ThrowsAsync<T>(Func<Task> action) where T : Exception;
}
```

**Internal API (rewriter targets):**
```csharp
public static class SharpInternal
{
    // Sync: expression tree (rich diagnostics)
    public static void Assert(Expression<Func<bool>> condition, string expr, string file, int line);

    // Async (general): no expression tree
    public static Task AssertAsync(Func<Task<bool>> conditionAsync, string expr, string file, int line);

    // Async (binary): capture left/right values
    public static Task AssertAsyncBinary(
        Func<Task<object?>> leftAsync, Func<Task<object?>> rightAsync,
        BinaryOp op, string expr, string file, int line);

    // Dynamic (general): limited diagnostics
    public static void AssertDynamic(Func<bool> condition, string expr, string file, int line);

    // Dynamic (binary): capture left/right via thunks
    public static void AssertDynamicBinary(
        Func<object?> left, Func<object?> right,
        BinaryOp op, string expr, string file, int line);
}
```

### Rewriter Decision Matrix

| Condition | Rewrite Target | Diagnostics |
|-----------|----------------|-------------|
| Sync (no await/dynamic) | `SharpInternal.Assert(() => expr)` | Rich (expression tree) |
| Await + binary | `AssertAsyncBinary(left, right, op)` | Values + diffs |
| Await + non-binary | `AssertAsync(() => expr)` | Minimal |
| Dynamic + binary | `AssertDynamicBinary(left, right, op)` | Values + diffs |
| Dynamic + non-binary | `AssertDynamic(() => expr)` | Minimal |
| Both await & dynamic | `AssertAsyncBinary` with dynamic parts | Best effort |

### Dependencies

**Core:**
- DiffPlex (string/sequence diffs)
- CompareNETObjects (object deep diffs)

**Optional:**
- PowerAssert (alternative mode via explicit overload - future)
- Verify.* (external diff viewers - future)

---

## Non-Goals

- Full AST explanation for async/dynamic beyond thunks
- Expression trees for await/dynamic (C# limitation)
- Rewriting outside test projects
- IDE integration/plugins
- Performance profiling tools

---

## Testing Commands

**Fast development cycle:**
```bash
./dev-test.sh    # Unit + integration tests
```

**Full validation:**
```bash
./test-local.sh  # All layers with package isolation
```

**See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed testing workflows.**
