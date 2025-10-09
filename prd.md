# SharpAssert PRD

**Status:** Active Development
**Scope:** Test projects (xUnit/NUnit/MSTest), .NET 9+
**License:** MIT

## Foundational Principles

**See [CONSTITUTION.md](CONSTITUTION.md)** for the immutable architectural principles and core philosophy.

---

## Remaining Work

### Verify.* Integration (External Diff Viewers)
**Status:** Not Started
**Goal:** When string/object diffs are large, write `.received`/`.verified` files and open configured diff tool.

**Implementation:**
- Integrate Verify.* packages (Verify.Xunit, Verify.NUnit, Verify.MSTest).
- Write assertion failure artifacts to `.received`/`.verified` files
- Open configured diff tool (VS Code, Beyond Compare, etc.)
- For NUnit/MSTest: attach files via `TestContext.AddTestAttachment` / `AddResultFile`

**Dependencies:**
```bash
dotnet add package Verify.Xunit  # or NUnit/MSTest variants
```

---

### Custom Formatter Registry
**Status:** Not Started
**Goal:** Allow users to register custom formatters for domain types.

**Example API:**
```csharp
SharpConfig.RegisterFormatter<MyType>((value, context) =>
{
    return $"MyType({value.Id}, {value.Name})";
});
```

---

### HTML Diff Emitter
**Status:** Not Started
**Goal:** Generate HTML diff artifacts for CI/CD pipelines.

**Implementation:**
- Render assertion failures as formatted HTML
- Include syntax highlighting, collapsible sections
- Write to artifacts directory for CI systems

---

### SourceLink-based Expression Embedding
**Status:** Not Started
**Goal:** Use SourceLink to embed original expression text without CallerArgumentExpression.

**Rationale:**
- More reliable than CallerArgumentExpression in edge cases
- Works across compilation boundaries

---

## Reference Documentation

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
- PowerAssert (alternative mode)
- Verify.* (external diff viewers - future)

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
