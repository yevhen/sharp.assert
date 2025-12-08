# SharpAssert IExpectation Migration Plan

## Overview

This document outlines the incremental migration plan for introducing the IExpectation architecture to SharpAssert. The migration is designed to be **non-breaking**, with old and new systems coexisting during the transition.

## Design Summary

### Core Interfaces

```csharp
// Flexible interface for any expectation
public interface IExpectation
{
    // Returns null on success, error message on failure
    string? Evaluate(ExpectationContext context);
}

// Context provided during evaluation
public class ExpectationContext
{
    public string Expression { get; init; }
    public string FilePath { get; init; }
    public int LineNumber { get; init; }
}

// Base class with operator overloading for composition
public abstract class Expectation : IExpectation
{
    public abstract string? Evaluate(ExpectationContext context);

    // Compose expectations with operators
    public static Expectation operator &(Expectation left, Expectation right)
        => new LogicalAndExpectation(left, right);

    public static Expectation operator |(Expectation left, Expectation right)
        => new LogicalOrExpectation(left, right);

    public static Expectation operator !(Expectation operand)
        => new LogicalNotExpectation(operand);
}
```

### Key Principles

1. **Single evaluation enforced by type system** - `Evaluate()` returns both result and message
2. **Composition via operators** - Use `&`, `|`, `!` to compose expectations
3. **Incremental migration** - Old and new systems coexist
4. **No breaking changes** - All existing tests continue to pass
5. **Reuse existing formatters** - IComparisonFormatter infrastructure preserved

## Migration Phases

---

## Phase 0: Foundation (Non-Breaking)

**Goal:** Establish the IExpectation architecture without breaking existing functionality.

### Tasks

#### 0.1: Create Core Interfaces
- [ ] Create `IExpectation` interface in `SharpAssert.Runtime/IExpectation.cs`
- [ ] Create `ExpectationContext` class in `SharpAssert.Runtime/ExpectationContext.cs`
- [ ] Create `Expectation` abstract base class in `SharpAssert.Runtime/Expectation.cs`
  - Implement `&`, `|`, `!` operators
  - Add XML documentation

#### 0.2: Create Logical Composition Expectations
- [ ] Create `LogicalAndExpectation` in `SharpAssert.Runtime/LogicalAndExpectation.cs`
  - Implement short-circuit evaluation in `Evaluate()`
  - Format failure messages showing which operand failed
- [ ] Create `LogicalOrExpectation` in `SharpAssert.Runtime/LogicalOrExpectation.cs`
  - Evaluate both operands on failure
  - Show why both were false
- [ ] Create `LogicalNotExpectation` in `SharpAssert.Runtime/LogicalNotExpectation.cs`
  - Negate the result
  - Format appropriate failure message

#### 0.3: Add New Assert Overload
- [ ] Add `Assert(IExpectation)` overload to `Sharp.cs`
  - Takes `IExpectation` parameter
  - Uses `CallerArgumentExpression` for expression text
  - Creates `ExpectationContext`
  - Calls `expectation.Evaluate(context)`
  - Throws `SharpAssertionException` if error returned
- [ ] Existing `Assert(Expression<Func<bool>>)` remains unchanged

#### 0.4: Create Tests for Foundation
- [ ] Create `ExpectationFoundationFixture.cs`
  - Test direct IExpectation usage
  - Test operator composition (&, |, !)
  - Test logical short-circuit behavior
  - Test error message formatting

#### 0.5: Commit
- [ ] Commit: "Add IExpectation foundation - no breaking changes"
- [ ] Verify all existing tests still pass
- [ ] Run full test suite

**Exit Criteria:**
- ✅ IExpectation interface defined
- ✅ Operator overloading works
- ✅ New Assert overload exists and works
- ✅ All existing tests pass (zero regressions)

---

## Phase 1: Convert to IExpectation (Wrap Existing Behavior)

**Goal:** Convert all Assert(Expression) calls to use IExpectation immediately, wrapping existing behavior in DefaultExpectation.

### Tasks

#### 1.1: Create Default Expectation (Wraps Existing Implementation)
- [ ] Create `DefaultExpectation` in `SharpAssert.Runtime/DefaultExpectation.cs`
  - Constructor takes `Expression<Func<bool>>` and expression text
  - `Evaluate()` delegates to EXISTING `ExpressionAnalyzer` (no new logic!)
  - Returns existing error formatting on failure
  - This preserves ALL current functionality exactly as-is
  ```csharp
  public override string? Evaluate(ExpectationContext context)
  {
      // Delegate to existing analyzer - zero changes to behavior
      var analyzer = new ExpressionAnalyzer();
      var result = analyzer.Analyze(expression, context);
      return result.IsSuccess ? null : result.ErrorMessage;
  }
  ```

#### 1.2: Create Expression Router
- [ ] Create `ExpressionToExpectation` class in `SharpAssert.Runtime/ExpressionToExpectation.cs`
  - Static method: `IExpectation FromExpression(Expression<Func<bool>> expr, string exprText)`
  - Initially returns `DefaultExpectation` for ALL cases:
  ```csharp
  public static IExpectation FromExpression(Expression<Func<bool>> expr, string exprText)
  {
      // Phase 1: Everything goes to DefaultExpectation
      // Later phases will add pattern matching here
      return new DefaultExpectation(expr, exprText);
  }
  ```

#### 1.3: Update Assert(Expression) to Use IExpectation
- [ ] Modify `SharpInternal.Assert(Expression<Func<bool>>)`:
  ```csharp
  public static void Assert(
      Expression<Func<bool>> expression,
      string? expressionText = null,
      [CallerFilePath] string? file = null,
      [CallerLineNumber] int line = 0)
  {
      // Convert expression to expectation
      var expectation = ExpressionToExpectation.FromExpression(expression, expressionText);

      // Delegate to IExpectation path
      var context = new ExpectationContext
      {
          Expression = expressionText ?? "",
          FilePath = file ?? "",
          LineNumber = line
      };

      var error = expectation.Evaluate(context);
      if (error != null)
          throw new SharpAssertionException(error);
  }
  ```
- [ ] Remove old direct call to ExpressionAnalyzer

#### 1.4: Verify Zero Breaking Changes
- [ ] Run ENTIRE test suite
- [ ] All 17 test fixtures must pass
- [ ] Error messages must be IDENTICAL to before
- [ ] No behavior changes whatsoever

#### 1.5: Commit
- [ ] Commit: "Convert Assert(Expression) to use IExpectation via DefaultExpectation wrapper"
- [ ] All tests pass
- [ ] Behavior unchanged
- [ ] Foundation ready for incremental specialization

**Exit Criteria:**
- ✅ All Assert(Expression) uses IExpectation
- ✅ DefaultExpectation wraps existing behavior
- ✅ All 17 test fixtures pass
- ✅ Error messages identical to before
- ✅ Zero breaking changes

---

## Phase 2: Binary Comparisons (First Specialization)

**Goal:** Replace DefaultExpectation with BinaryComparisonExpectation for binary operators.

### Tasks

#### 2.1: Create Binary Comparison Expectation
- [ ] Create `BinaryComparisonExpectation` in `SharpAssert.Runtime/BinaryComparisonExpectation.cs`
  - Constructor takes `BinaryExpression` and expression text
  - `Evaluate()` compiles left and right operands once
  - Performs comparison based on `NodeType`
  - On failure, delegates to `ComparisonFormatterService.GetFormatter()`
  - Reuses existing formatter infrastructure (StringComparisonFormatter, ObjectComparisonFormatter, etc.)

#### 2.2: Update Expression Router (Add First Pattern)
- [ ] Update `ExpressionToExpectation.FromExpression()`:
  ```csharp
  public static IExpectation FromExpression(Expression<Func<bool>> expr, string exprText)
  {
      var body = expr.Body;

      return body switch
      {
          // NEW: Route binary comparisons to specialized expectation
          BinaryExpression binary when IsBinaryComparison(binary)
              => new BinaryComparisonExpectation(binary, exprText),

          // Fallback to existing behavior (wrapped)
          _ => new DefaultExpectation(expr, exprText)
      };
  }

  static bool IsBinaryComparison(BinaryExpression binary)
      => binary.NodeType is ExpressionType.Equal
          or ExpressionType.NotEqual
          or ExpressionType.LessThan
          or ExpressionType.LessThanOrEqual
          or ExpressionType.GreaterThan
          or ExpressionType.GreaterThanOrEqual;
  ```

#### 2.3: Verify Existing Tests Pass
- [ ] Run `BinaryComparisonFixture` - should pass
- [ ] Compare error messages - must be identical
- [ ] String comparisons use `StringComparisonFormatter`
- [ ] Object comparisons use `ObjectComparisonFormatter`
- [ ] Collection comparisons use `CollectionComparisonFormatter`
- [ ] Nullable comparisons use `NullableComparisonFormatter`

#### 2.4: Run Full Test Suite
- [ ] All 17 test fixtures must still pass
- [ ] Only binary comparisons use new path
- [ ] Everything else still uses DefaultExpectation (unchanged)

#### 2.5: Commit
- [ ] Commit: "Replace DefaultExpectation with BinaryComparisonExpectation for binary operators"
- [ ] All tests pass
- [ ] Error messages identical
- [ ] First specialization complete

**Exit Criteria:**
- ✅ Binary comparisons use specialized expectation
- ✅ Existing formatters reused correctly
- ✅ Error messages identical to DefaultExpectation behavior
- ✅ All test fixtures still pass
- ✅ DefaultExpectation handles everything except binary comparisons

---

## Phase 3: Verify String/Object/Collection/Nullable Handling

**Goal:** Verify that BinaryComparisonExpectation correctly delegates to specialized formatters.

**Note:** These don't need separate expectations - they're handled by BinaryComparisonExpectation delegating to the existing formatter infrastructure.

### Tasks

#### 3.1: Verify String Comparisons
- [ ] Run `StringComparisonFixture` - should pass
- [ ] Verify inline diff: `h[-e][+a]llo`
- [ ] Verify multiline diff formatting
- [ ] Verify null vs empty string handling
- [ ] Verify long string truncation
- [ ] Confirm delegates to `StringComparisonFormatter`

#### 3.2: Verify Object Comparisons
- [ ] Run `ObjectComparisonFixture` - should pass
- [ ] Verify nested object comparison
- [ ] Verify property difference reporting
- [ ] Verify custom Equals() respect
- [ ] Verify null object handling
- [ ] Verify limit of 20 differences
- [ ] Confirm delegates to `ObjectComparisonFormatter`

#### 3.3: Verify Collection Comparisons
- [ ] Run `CollectionComparisonFixture` - should pass
- [ ] Verify first mismatch reporting
- [ ] Verify missing/extra element detection
- [ ] Verify empty collection handling
- [ ] Verify DateTime formatting with InvariantCulture
- [ ] Confirm delegates to `CollectionComparisonFormatter`

#### 3.4: Verify Nullable Type Handling
- [ ] Run `NullableTypeFixture` - should pass
- [ ] Verify int?, bool?, DateTime? handling
- [ ] Verify nullable vs non-nullable comparisons
- [ ] Verify nullable vs nullable comparisons
- [ ] Verify HasValue state display
- [ ] Confirm delegates to `NullableComparisonFormatter`

#### 3.5: Commit
- [ ] Commit: "Verify formatter delegation in BinaryComparisonExpectation"
- [ ] All specialized formatter fixtures pass
- [ ] No code changes needed (already working)

**Exit Criteria:**
- ✅ String/Object/Collection/Nullable fixtures all pass
- ✅ Formatter delegation works correctly
- ✅ Error messages identical to previous behavior

---

## Phase 4: Logical Operators (&&, ||, !)

**Goal:** Replace DefaultExpectation with composition expectations for logical operators.

### Tasks

#### 4.1: Update Expression Router for Logical Operators
- [ ] Update `ExpressionToExpectation.FromExpression()`:
  ```csharp
  public static IExpectation FromExpression(Expression<Func<bool>> expr, string exprText)
  {
      var body = expr.Body;

      return body switch
      {
          // Binary comparisons (Phase 2)
          BinaryExpression binary when IsBinaryComparison(binary)
              => new BinaryComparisonExpectation(binary, exprText),

          // NEW: Logical operators
          BinaryExpression binary when binary.NodeType == ExpressionType.AndAlso
              => new LogicalAndExpectation(
                  FromExpression(Expression.Lambda<Func<bool>>(binary.Left), null),
                  FromExpression(Expression.Lambda<Func<bool>>(binary.Right), null)),

          BinaryExpression binary when binary.NodeType == ExpressionType.OrElse
              => new LogicalOrExpectation(
                  FromExpression(Expression.Lambda<Func<bool>>(binary.Left), null),
                  FromExpression(Expression.Lambda<Func<bool>>(binary.Right), null)),

          UnaryExpression unary when unary.NodeType == ExpressionType.Not
              => new LogicalNotExpectation(
                  FromExpression(Expression.Lambda<Func<bool>>(unary.Operand), null)),

          // Fallback
          _ => new DefaultExpectation(expr, exprText)
      };
  }
  ```

#### 4.2: Test Logical Operators
- [ ] Run `LogicalOperatorFixture` - should pass
- [ ] Verify && short-circuit behavior
- [ ] Verify || both operand evaluation
- [ ] Verify ! negation
- [ ] Verify error messages match previous behavior
- [ ] Verify composition: expressions can be binary comparisons OR logical operators (recursive)

#### 4.3: Commit
- [ ] Commit: "Replace DefaultExpectation with composition expectations for logical operators"
- [ ] `LogicalOperatorFixture` passes
- [ ] All other fixtures still pass

**Exit Criteria:**
- ✅ Logical operators compose expectations recursively
- ✅ Short-circuit semantics preserved during evaluation
- ✅ LogicalOperatorFixture passes
- ✅ DefaultExpectation now handles everything except binary comparisons and logical operators

---

## Phase 5: LINQ Operations

**Goal:** Replace DefaultExpectation with LINQ operation expectations (Contains, Any, All).

### Tasks

#### 5.1: Create LINQ Expectations
- [ ] Create `LinqContainsExpectation` in `SharpAssert.Runtime/LinqContainsExpectation.cs`
  - Extract collection and item from MethodCallExpression
  - Format failure with collection preview
  - Reuse formatting logic from `LinqOperationFormatter`
- [ ] Create `LinqAnyExpectation`
  - Handle empty collection case
  - Show predicate expression
  - Format failure message
- [ ] Create `LinqAllExpectation`
  - Compile and evaluate predicate to find failures
  - Show only failing items
  - Limit to 10 items

#### 5.2: Update Expression Router
- [ ] Add pattern matching for LINQ methods in ExpressionToExpectation
- [ ] Add before the fallback case

#### 5.3: Test LINQ Operations
- [ ] Run `LinqOperationsFixture` - should pass
- [ ] Verify Contains formatting
- [ ] Verify Any predicate display
- [ ] Verify All failing items
- [ ] Verify collection truncation at 10 items

#### 5.4: Commit
- [ ] Commit: "Replace DefaultExpectation with LINQ operation expectations"
- [ ] `LinqOperationsFixture` passes
- [ ] All other fixtures still pass

**Exit Criteria:**
- ✅ LINQ operations use specialized expectations
- ✅ Error messages identical to previous behavior
- ✅ LinqOperationsFixture passes

---

## Phase 6: SequenceEqual

**Goal:** Replace DefaultExpectation with SequenceEqual expectation.

### Tasks

#### 6.1: Create SequenceEqual Expectation
- [ ] Create `SequenceEqualExpectation` in `SharpAssert.Runtime/SequenceEqualExpectation.cs`
  - Extract both sequences from MethodCallExpression
  - Materialize to lists
  - Use DiffPlex for unified diff
  - Show length mismatch if applicable
  - Detect custom comparer usage
  - Context lines: 3 before each diff
  - Max diff lines: 50
  - Max sequence preview: 20 items

#### 6.2: Update Expression Router
- [ ] Add pattern matching for SequenceEqual in ExpressionToExpectation

#### 6.3: Test SequenceEqual
- [ ] Run `SequenceEqualFixture` - should pass
- [ ] Verify unified diff display
- [ ] Verify length mismatch reporting
- [ ] Verify custom comparer indication
- [ ] Verify diff truncation at 50 lines

#### 6.4: Commit
- [ ] Commit: "Replace DefaultExpectation with SequenceEqual expectation"
- [ ] `SequenceEqualFixture` passes
- [ ] All other fixtures still pass

**Exit Criteria:**
- ✅ SequenceEqual uses specialized expectation
- ✅ Error messages identical to previous behavior
- ✅ SequenceEqualFixture passes

---

## Phase 7: Async Support

**Goal:** Handle async binary comparisons (rewriter already handles this).

**Note:** Async support is primarily handled by the rewriter generating appropriate code. The expectations just need to handle the result.

### Tasks

#### 7.1: Verify Async Works
- [ ] Run `AsyncBinaryFixture` - should pass
- [ ] Run `AsyncAssertionFixture` - should pass
- [ ] Async is handled by rewriter, not expression tree
- [ ] Already working through existing async analyzers

#### 7.2: Document Async Handling
- [ ] Add note that async is rewriter-level, not expectation-level
- [ ] Existing `AsyncExpressionAnalyzer` wrapped by DefaultExpectation
- [ ] May need future refactoring but works for now

#### 7.3: Commit
- [ ] Commit: "Verify async support works through DefaultExpectation"
- [ ] Async fixtures pass
- [ ] No changes needed (already working)

**Exit Criteria:**
- ✅ Async fixtures pass
- ✅ Async handled by rewriter + DefaultExpectation
- ✅ Future work documented if needed

---

## Phase 8: Dynamic Support

**Goal:** Handle dynamic type comparisons (similar to async).

**Note:** Dynamic support is handled by the existing `DynamicExpressionAnalyzer` wrapped by DefaultExpectation.

### Tasks

#### 8.1: Verify Dynamic Works
- [ ] Run `DynamicAssertionFixture` - should pass
- [ ] Dynamic handled by existing analyzer wrapped by DefaultExpectation
- [ ] Already working through existing dynamic analyzers

#### 8.2: Commit
- [ ] Commit: "Verify dynamic support works through DefaultExpectation"
- [ ] Dynamic fixture passes
- [ ] No changes needed (already working)

**Exit Criteria:**
- ✅ DynamicAssertionFixture passes
- ✅ Dynamic handled by existing analyzer + DefaultExpectation
- ✅ Future work documented if needed

---

## Phase 9: Verify Complete Migration

**Goal:** Verify all 17 test fixtures pass with the new architecture.

### Tasks

#### 9.1: Run Complete Test Suite
- [ ] Run ALL 17 test fixtures
- [ ] Verify all pass
- [ ] Compare error messages against baseline (should be identical)
- [ ] Check performance (should be similar or better)

#### 9.2: Test Coverage Analysis
- [ ] Run test coverage for new expectation classes
- [ ] Ensure all branches covered
- [ ] Add missing tests if needed

#### 9.3: Analyze DefaultExpectation Usage
- [ ] Check which assertions still use DefaultExpectation
- [ ] Should only be edge cases and features not yet migrated
- [ ] Document what remains

#### 9.4: Commit
- [ ] Commit: "Verify complete test suite passes with IExpectation architecture"
- [ ] All tests pass
- [ ] Coverage maintained or improved

**Exit Criteria:**
- ✅ All 17 test fixtures pass
- ✅ Error messages match previous implementation
- ✅ No performance regressions
- ✅ Test coverage maintained

---

## Phase 10: Refactor Throws to IExpectation

**Goal:** Migrate Throws/ThrowsAsync to return IExpectation (breaking change, but worth it).

### Tasks

#### 10.1: Create Throws Expectations
- [ ] Create `ThrowsExpectation<T>` in `SharpAssert.Runtime/ThrowsExpectation.cs`
  - Inherits from `Expectation` (for operator overloading)
  - Evaluate action in constructor (eager evaluation)
  - Capture exception or null
  - Implement `Evaluate()` to format based on captured state
  - Expose `Exception` property for further assertions
  - Add implicit conversion to exception type: `public static implicit operator T(ThrowsExpectation<T> exp)`
- [ ] Create `ThrowsAsyncExpectation<T>` similarly for async

#### 10.2: Update Sharp.Throws
- [ ] Modify `Sharp.Throws<T>(Action)` to return `ThrowsExpectation<T>`
- [ ] Modify `Sharp.ThrowsAsync<T>(Func<Task>)` to return `ThrowsAsyncExpectation<T>`
- [ ] This is a BREAKING CHANGE but enables:
  - `Assert(Throws<T>(...))` for direct assertion
  - `ArgumentException ex = Throws<ArgumentException>(...)` for implicit conversion
  - `Assert(Throws<T>(...) & otherExpectation)` for composition

#### 10.3: Update Tests
- [ ] Update tests using Throws to work with new return type
- [ ] Test implicit conversion: `ArgumentException ex = Throws<ArgumentException>(...)`
- [ ] Test direct assertion: `Assert(Throws<ArgumentException>(...))`
- [ ] Test composition: `Assert(Throws<ArgumentException>(...) & !Throws<InvalidOperationException>(...))`
- [ ] Verify exception property access works

#### 10.4: Update CLAUDE.md
- [ ] Document breaking change
- [ ] Add migration guide for users

#### 10.5: Commit
- [ ] Commit: "Refactor Throws to return IExpectation (BREAKING CHANGE)"
- [ ] All exception-related tests pass
- [ ] Breaking change documented

**Exit Criteria:**
- ✅ Throws returns Expectation subclass
- ✅ Exception property accessible
- ✅ Implicit conversion works
- ✅ Composition with operators works
- ✅ All tests pass
- ✅ Breaking change documented

---

## Phase 11: Add Extension Method Expectations (Demo)

**Goal:** Demonstrate extension method pattern with EquivalentTo (first of many).

### Tasks

#### 11.1: Create EquivalentTo Extension
- [ ] Create `ExpectationExtensions` class in `SharpAssert.Runtime/ExpectationExtensions.cs`
- [ ] Add `EquivalentTo<T>(this T left, T right)` extension method
- [ ] Returns `ObjectEquivalenceExpectation<T>`
- [ ] Uses CompareNetObjects for comparison

#### 11.2: Create ObjectEquivalenceExpectation
- [ ] Create `ObjectEquivalenceExpectation<T>` in `SharpAssert.Runtime/ObjectEquivalenceExpectation.cs`
  - Inherits from `Expectation` (for operator overloading)
  - Constructor takes left and right values
  - `Evaluate()` uses CompareLogic for deep comparison
  - Returns null on success, diff string on failure

#### 11.3: Add Tests
- [ ] Create `EquivalentToFixture.cs`
- [ ] Test basic object equivalence
- [ ] Test composition: `Assert(x.EquivalentTo(y) & z.EquivalentTo(w))`
- [ ] Test negation: `Assert(!x.EquivalentTo(y))`
- [ ] Test complex composition: `Assert(x.EquivalentTo(y) | !z.EquivalentTo(w))`

#### 11.4: Document Pattern
- [ ] Add example to README.md
- [ ] Show how to create custom expectations
- [ ] Document operator composition

#### 11.5: Commit
- [ ] Commit: "Add EquivalentTo extension method as first FluentAssertions-style expectation"
- [ ] New tests pass
- [ ] Pattern documented for future extensions

**Exit Criteria:**
- ✅ EquivalentTo works and composes naturally
- ✅ Pattern documented for community contributions
- ✅ Foundation for future FluentAssertions-style expectations

---

## Phase 12: Optional Cleanup and Optimization

**Goal:** Consider removing DefaultExpectation wrapper if most cases are specialized (OPTIONAL).

**Note:** This phase is optional. DefaultExpectation provides a safety net and may still be useful for edge cases.

### Tasks

#### 12.1: Analyze DefaultExpectation Usage
- [ ] Check what assertions still use DefaultExpectation
- [ ] If usage is minimal, consider removing wrapper entirely
- [ ] If usage is significant, keep DefaultExpectation as fallback

#### 12.2: Option A: Keep DefaultExpectation (Recommended)
- [ ] Keep as fallback for unhandled cases
- [ ] Provides safety net
- [ ] Easy to add new specializations later
- [ ] No changes needed

#### 12.3: Option B: Remove DefaultExpectation (If Minimal Usage)
- [ ] Remove DefaultExpectation class
- [ ] Remove ExpressionAnalyzer wrapper
- [ ] Ensure ALL cases handled by specialized expectations
- [ ] More work but cleaner architecture

#### 12.4: Update Documentation
- [ ] Update README.md with IExpectation architecture
- [ ] Document extension method pattern
- [ ] Document operator composition (&, |, !)
- [ ] Show examples of custom expectations

#### 12.5: Update CLAUDE.md
- [ ] Add IExpectation design notes
- [ ] Add patterns for creating expectations
- [ ] Document formatter reuse strategy
- [ ] Note that async/dynamic still use old analyzers (for now)

#### 12.6: Commit
- [ ] Commit: "Update documentation for IExpectation architecture"
- [ ] Documentation complete
- [ ] Examples clear

**Exit Criteria:**
- ✅ Decision made on DefaultExpectation
- ✅ Documentation updated
- ✅ Examples provided
- ✅ Contributing guide updated

---

## Success Criteria for Complete Migration

### Functionality
- ✅ All 17 test fixtures pass
- ✅ Error messages identical to original implementation
- ✅ No performance regressions
- ✅ Cross-platform compatibility maintained (InvariantCulture, etc.)

### Code Quality
- ✅ Clean separation of concerns (expectations vs formatters)
- ✅ Easy to add new expectations
- ✅ Operator composition works naturally
- ✅ Single evaluation enforced by type system

### Testing
- ✅ Test coverage maintained or improved
- ✅ All existing tests pass without modification (except Throws return type)
- ✅ New tests for expectation-specific features

### Documentation
- ✅ Architecture documented
- ✅ Examples provided
- ✅ Contributing guide updated

---

## Rollback Plan

If issues arise during migration:

1. **Any phase (Phases 0-12):**
   - Revert to previous commit
   - Fix issues in isolated branch
   - Re-apply when fixed
   - Each phase is independently testable

2. **No feature flags = simpler rollback:**
   - Just use git revert
   - No configuration management
   - No dual code paths to maintain

3. **Progressive specialization = safety:**
   - DefaultExpectation always works as fallback
   - New expectations only added when verified
   - Can pause migration at any phase

---

## Timeline Estimate

**Conservative estimate:** 1.5-2 weeks for core migration

- **Phase 0:** 1 day (foundation: interface, base class, operators, composition)
- **Phase 1:** 0.5 days (DefaultExpectation wrapper - trivial)
- **Phase 2:** 1-2 days (binary comparisons - first real specialization)
- **Phase 3:** 0.5 days (verify formatters work - no code changes)
- **Phase 4:** 1 day (logical operators composition)
- **Phase 5:** 1 day (LINQ operations)
- **Phase 6:** 0.5 days (SequenceEqual)
- **Phases 7-8:** 0.5 days (verify async/dynamic - no changes)
- **Phase 9:** 0.5 days (full test suite verification)
- **Phase 10:** 1-2 days (Throws refactor - breaking change)
- **Phase 11:** 1 day (EquivalentTo extension example)
- **Phase 12:** 0.5 days (optional cleanup/docs)

**Aggressive estimate:** 1 week with focused effort

**Key insight:** Most phases are verification, not implementation. The wrapper approach means less new code to write.

---

## Notes

- Each phase should be a separate commit
- Run full test suite after each phase
- Keep commits small and focused
- Document any deviations from plan
- Update this document as you discover issues

---

## Future Work (Post-Migration)

Once migration is complete, these features can be added:

1. **FluentAssertions-style expectations:**
   - `InAscendingOrder()`
   - `HasUniqueItems()`
   - `Matches(pattern)` for string wildcards
   - `CloseTo(expected, tolerance)` for numeric proximity
   - Custom object comparison with configuration

2. **Enhanced Throws:**
   - `Throws<T>().WithMessage(pattern)`
   - `Throws<T>().WithInnerException<T2>()`
   - Message pattern matching

3. **Performance optimizations:**
   - Cache compiled expressions
   - Optimize formatter selection

4. **Additional features:**
   - Custom expectations from users
   - Plugin system for third-party expectations
   - Better integration with test frameworks

---

## References

- `expectation-classes-research.md` - Detailed analysis of 17 expectation classes needed
- `fluent.md` - FluentAssertions features reference
- `learnings.md` - Implementation learnings and gotchas
- Test fixtures in `src/SharpAssert.Tests/` - Current behavior to preserve
