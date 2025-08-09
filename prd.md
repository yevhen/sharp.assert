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

For each `InvocationExpression` resolving to `Sharp.Assert(bool)`:
1.  **Analyze the argument with `SemanticModel`:**
    - Contains `await`? → `HasAwait = true`
    - Contains `dynamic` ops? → `HasDynamic = true`
    - Top‑level binary operator? (`Eq`/`Ne`/`Lt`/`Le`/`Gt`/`Ge`) → `BinaryOp?`
2.  **Rewrite decision matrix**

| Case                 | Rewrite to                                                                                             | Notes                                                      |
| -------------------- | ------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------- |
| No `await`, no `dynamic` | `SharpInternal.Assert(() => <expr>, expr, file, line)`                                                 | Main sync path (expression tree)                           |
| `await` + binary       | `SharpInternal.AssertAsyncBinary(async () => <left>, async () => <right>, op, expr, file, line)`         | Left/right are awaitable subexpressions; ensure order      |
| `await` + not binary   | `SharpInternal.AssertAsync(async () => <expr>, expr, file, line)`                                      | Limited diagnostics                                        |
| `dynamic` + binary     | `SharpInternal.AssertDynamicBinary(() => (object?)<left>, () => (object?)<right>, op, expr, file, line)` | Better diagnostics with values                             |
| `dynamic` + not binary | `SharpInternal.AssertDynamic(() => <expr>, expr, file, line)`                                          | Minimal diagnostics                                        |
| Both `await` & `dynamic` | Prefer async track; if binary → `AssertAsyncBinary` using thunks that include dynamic parts.           |                                                            |

3.  **Emit fully‑qualified `global::SharpInternal.*` calls.**
    (No `#line` needed — we aren’t expanding large blocks. Debugging stays natural.)

**Note:** You can include the raw `expr` string using `[CallerArgumentExpression]` from the original `Sharp.Assert`, or the rewriter can embed it literally. Prefer embedding the literal for clarity.

---

## 5. Output Formatting & Limits
- **Header:** `Assertion failed: <expr>  at <file>:<line>`
- **Binary:** print semantic operator and both values.
- **Strings:** DiffPlex inline (default). Configurable side‑by‑side and context lines.
- **Collections:** first mismatch index; missing/extra; preview first/last N; avoid gigantic dumps.
- **Objects:** list up to M property‑level diffs (path → left vs right).
- **Truncation:** apply global limits; append `…` (truncated).

---

## 6. Configuration

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
    public static SharpOptions Global { get; set; } = new();
}

public enum CollectionEqualityMode { Strict, Equivalent }
```
(Per‑call overrides can be added later, e.g., via a scoped `SharpScope.With(options)`.)

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

## 8. Walking Skeleton → Iterative Plan

- **Iteration 0 — Walking Skeleton**
    - Ship `Sharp.Assert(bool)` (fallback).
    - MSBuild task that only rewrites to `SharpInternal.Assert(() => expr, …)` for sync cases (no `await`/`dynamic`).
    - Runtime: basic visitor for expression trees:
        - Support: binary compares, logical ops, method calls, member access.
        - On failure: print `<expr>` + left/right for binaries.

- **Iteration 1 — Strings & Collections**
    - Integrate DiffPlex for string diffs (inline).
    - Detect `IEnumerable` vs `IEnumerable`; show first mismatch, missing/extra.
    - Add preview (first/last N), truncation.

- **Iteration 2 — Objects (deep diff)**
    - Integrate Compare‑Net‑Objects; render path‑level diffs.
    - Cap differences by `MaxDiffLines`.

- **Iteration 3 — Membership/LINQ**
    - In expression‑tree visitor, detect `.Contains`, `.Any`, `.All`, `.SequenceEqual`.
    - For `.Contains`/`.Any`/`.All`: enumerate once to `List<T>`; show counts, matching subsets.
    - For `.SequenceEqual`: side‑by‑side unified diff (DiffPlex).

- **Iteration 4 — Async support**
    - Rewriter: detect `await`; for binary emit `AssertAsyncBinary(leftThunk, rightThunk, op, …)`; else `AssertAsync`.
    - Runtime:
        - `AssertAsyncBinary`: `await` left→right; render values/diffs.
        - `AssertAsync`: minimal diagnostics (`expr` + `False`) initially.

- **Iteration 5 — Dynamic support**
    - Rewriter: detect `dynamic`; for binary emit `AssertDynamicBinary(leftThunk, rightThunk, op, …)`; else `AssertDynamic`.
    - Runtime:
        - `AssertDynamicBinary`: evaluate thunks; `(dynamic)left OP (dynamic)right`; show values/diffs.
        - `AssertDynamic`: minimal diagnostics (`expr` + `False`).

- **Iteration 6 — Polish & Config**
    - Global options; side‑by‑side string diffs; collection `Equivalent` mode via FA.
    - `Verify` integration (optional) for large diffs.
    - Docs + samples for xUnit/NUnit/MSTest; CI (build, tests, pack).

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
- **Integration:**
    - Real test project with `EnableSharpLambdaRewrite=true`; run & verify messages.
    - Large collections/strings ensure truncation works.

---

## 10. Mapping Table (rewrite rules)

| Case                 | User Code                      | Emits                                                                                             |
| -------------------- | ------------------------------ | ------------------------------------------------------------------------------------------------- |
| Sync, normal         | `Assert(a + 2 == b * 3)`       | `SharpInternal.Assert(() => a + 2 == b * 3, "a + 2 == b * 3", file, line)`                        |
| Sync, logical        | `Assert(isAuth && hasPerms)`   | `SharpInternal.Assert(() => isAuth && hasPerms, "...", file, line)`                                |
| `.Contains`          | `Assert(users.Contains(admin))`| `SharpInternal.Assert(() => users.Contains(admin), "...", file, line)`                            |
| `.Any` / `.All`      | `Assert(items.Any(p))`         | `SharpInternal.Assert(() => items.Any(p), "...", file, line)`                                     |
| `.SequenceEqual`     | `Assert(a.SequenceEqual(b))`   | `SharpInternal.Assert(() => a.SequenceEqual(b), "...", file, line)`                               |
| Async binary         | `Assert(await L() == await R())`| `SharpInternal.AssertAsyncBinary(async () => await L(), async () => await R(), Eq, "...", file, line)` |
| Async general        | `Assert(await CheckAsync(x))`  | `SharpInternal.AssertAsync(async () => await CheckAsync(x), "...", file, line)`                   |
| Dynamic binary       | `Assert(xDyn == y)`            | `SharpInternal.AssertDynamicBinary(() => (object?)xDyn, () => (object?)y, Eq, "...", file, line)`  |
| Dynamic general      | `Assert(xDyn.Call() > 0)`      | `SharpInternal.AssertDynamic(() => xDyn.Call() > 0, "...", file, line)`                           |
| Both `await` & `dynamic` | `Assert(await xDyn.F() == y)`  | Prefer async binary thunks that return `object?`                                                  |

---

## 11. Non‑Goals (initial)
- Full AST explanation for `async` and `dynamic` (beyond the targeted thunks).
- Expression‑trees for `await` / `dynamic` (not supported by C#).
- Rewriting outside test projects (default opt‑in per test `csproj`).

---

## 12. Acceptance Criteria
- Devs write only `Sharp.Assert(bool)`; rewriter does the rest.
- Sync cases yield PowerAssert‑grade messages: left/right, logical operand values, membership/LINQ insights, `SequenceEqual` diff, string/collection/object diffs.
- Async/dynamic covered with targeted thunks; at minimum, clear failure text; for binaries — proper value diffs.
- No double evaluation; order preserved.
- Easy to read messages, truncated sensibly; optional external diff viewer.

---

## 13. Nice‑to‑Have (future)
- Per‑call options (`Assert(…, new SharpOptions { … })`).
- Custom printers/formatters registry for domain types.
- HTML diff emitter (for CI artifacts).
- SourceLink‑based embedding of original `expr` text (if needed).

---

This PRD gives a clear, minimal rewrite strategy (wrap to lambda / emit async/dynamic thunks) and a rich runtime that mirrors pytest’s UX, including great diffs for strings, collections, sequences, and deep object graphs. The walking skeleton can be implemented fast, then iterated safely.
