# SharpAssert Constitution

This document defines the immutable architectural principles and core philosophy that guide all SharpAssert development.

---

## 1. Hybrid Architecture: Minimal Rewriter, Rich Runtime

**Principle**: Keep the rewriter minimal; move heavy lifting to runtime.

**Why**: Small, focused rewriter reduces build complexity; rich runtime provides powerful diagnostics.

**Invariant**: Rewriter only performs syntactic transformations (wrapping). All semantic analysis happens at runtime.

---

## 2. Never Break the Build

**Principle**: If rewriter analysis fails for any reason, silently leave the original `Sharp.Assert` call untouched.

**Why**: Graceful fallback to `CallerArgumentExpression` prevents build failures from complex or unexpected user code.

**Invariant**: Rewriter errors must never propagate to user builds.

---

## 3. Single Evaluation

**Principle**: Each sub-expression is evaluated exactly once; source order is preserved.

**Why**: Side effects must be predictable. Left-to-right evaluation matches user expectations.

**Invariant**: No double evaluation. Left before right, always.

---

## 4. Single Materialization

**Principle**: Collections are materialized exactly once to avoid re-enumeration.

**Why**: Re-enumeration can cause performance issues, side effects, or state changes.

**Invariant**: Materialize on first access, cache the result.

---

## 5. Simple API, Rich Diagnostics

**Principle**: Users write only `Sharp.Assert(bool)`. Everything else is internal.

**Why**: One entry point keeps the API simple; rewriter handles routing to specialized internal overloads.

**Invariant**: Public surface is minimal. Internal complexity is hidden from users.

---

## 6. Pay-on-Failure Performance

**Principle**: Rich diagnostic tools are only invoked on assertion failure.

**Why**: Passing tests have near-zero overhead; detailed diagnostics are computed only when needed.

**Invariant**: No preemptive diff computation. Lazy evaluation of diagnostics.

---

## 7. Readable Output, Sensible Truncation

**Principle**: Easy to read messages, truncated sensibly.

**Why**: Assertion failures should be quickly understood. Gigantic outputs obscure the problem.

**Invariant**: Apply global limits; append `…` for truncation.

---

## 8. Type-Aware Diagnostics

**Principle**: Comparison diagnostics use best-fit formatters based on runtime type.

**Why**: Strings need character diffs, collections need index reports, objects need property diffs.

**Invariant**: Route to appropriate formatter based on detected type.

---

## 9. Expression Trees for Sync, Thunks for Async/Dynamic

**Principle**: Use expression trees for sync paths; use thunks for await/dynamic.

**Why**: C# expression trees don't support await/dynamic. Thunks enable value capture without expression tree constraints.

**Invariant**: Sync → `Expression<Func<bool>>`. Async/dynamic → `Func<Task<object?>>` or `Func<object?>`.

---

## 11. No Defensive/Speculative Code

**Principle**: Every line of code must be reachable via public API. No "just in case" code.

**Why**: Defensive code masks bugs, adds complexity, and degrades maintainability.

**Invariant**: Trust the type system. Let exceptions bubble up naturally.

---

## 12. Critical Invariants

**These rules must NEVER be violated:**

1. **Never break the build**: Rewriter failures must fall back gracefully
2. **Evaluate once**: No double evaluation of sub-expressions
3. **Enumerate once**: No re-enumeration of collections
4. **Source order**: Left before right, always
5. **Public API simplicity**: One entry point (`Sharp.Assert`)
6. **Type safety**: No defensive coding; trust the type system
7. **Testing isolation**: Package tests use isolated caches
8. **Pay on failure**: Diagnostics computed only when assertions fail