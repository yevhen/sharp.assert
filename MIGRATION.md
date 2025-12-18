# SharpAssert Migration Guide

Pytest-style assertions using native C# syntax. MSBuild compile-time rewriting provides rich diagnostics without special syntax.

## Core Concept

```csharp
// Other frameworks
actual.Should().Be(expected);
Assert.AreEqual(expected, actual);

// SharpAssert - native C# expressions
Assert(actual == expected);
```

Basic setup:
```csharp
using static SharpAssert.Sharp;
```

## API

```csharp
Assert(bool condition [, string message])
TException Throws<TException>(Action action [, string message])
Task<TException> ThrowsAsync<TException>(Func<Task> action [, string message])
```

## Native C# Support

SharpAssert works with standard C# - no special syntax required:

```csharp
// Comparisons
Assert(actual == expected);  // ==, !=, <, >, <=, >=

// Logical - evaluates ALL operands, shows all failures
Assert(x > 0 && y < 100);   // &&, ||, !

// LINQ
Assert(items.Contains(value));
Assert(items.Any(x => x.Valid));
Assert(items.All(x => x.Valid));
Assert(actual.SequenceEqual(expected));

// Strings (auto-diff on ==)
Assert(str == expected);
Assert(str.StartsWith(prefix));
Assert(str.EndsWith(suffix));
Assert(str.Contains(substring));

// Async
Assert(await GetAsync() == expected);

// Exceptions
Assert(Throws<ArgumentException>(() => code));
Assert(await ThrowsAsync<InvalidOperationException>(() => asyncCode));
```

## Custom Expectations

For reusable patterns not covered above:

```csharp
sealed class IsEvenExpectation(int value) : Expectation
{
    public override EvaluationResult Evaluate(ExpectationContext context) =>
        value % 2 == 0
            ? ExpectationResults.Pass(context.Expression)
            : ExpectationResults.Fail(context.Expression, $"Expected even, got {value}");
}

// Usage
Assert(IsEven(4));
```

## Anti-Patterns

```csharp
Assert(condition == true);        // Bad → Assert(condition)
Assert(value == false);           // Bad → Assert(!value)
Assert(x.Equals(y));              // Bad → Assert(x == y)
try { code; Assert(false); }      // Bad → Assert(Throws<T>(() => code))
```

---

# Framework Migration

## NUnit / xUnit / MSTest

```csharp
// Basic
Assert.True(condition)                    → Assert(condition)
Assert.False(condition)                   → Assert(!condition)
Assert.AreEqual(expected, actual)         → Assert(actual == expected)
Assert.AreNotEqual(expected, actual)      → Assert(actual != expected)
Assert.IsNull(actual)                     → Assert(actual == null)
Assert.IsNotNull(actual)                  → Assert(actual != null)
Assert.Greater(actual, expected)          → Assert(actual > expected)
Assert.Less(actual, expected)             → Assert(actual < expected)
Assert.That(actual, Is.EqualTo(expected)) → Assert(actual == expected)

// Strings
Assert.That(str, Does.StartWith(prefix))  → Assert(str.StartsWith(prefix))
Assert.That(str, Does.EndWith(suffix))    → Assert(str.EndsWith(suffix))
Assert.That(str, Does.Contain(substring)) → Assert(str.Contains(substring))
StringAssert.Contains(substring, str)     → Assert(str.Contains(substring))

// Collections
CollectionAssert.Contains(coll, item)     → Assert(coll.Contains(item))
CollectionAssert.AreEqual(exp, actual)    → Assert(actual.SequenceEqual(exp))
CollectionAssert.IsEmpty(coll)            → Assert(!coll.Any())
CollectionAssert.IsNotEmpty(coll)         → Assert(coll.Any())
Assert.That(coll, Has.Count.EqualTo(n))   → Assert(coll.Count() == n)

// Exceptions
Assert.Throws<T>(() => code)              → Assert(Throws<T>(() => code))
Assert.ThrowsAsync<T>(() => asyncCode)    → Assert(await ThrowsAsync<T>(() => asyncCode))

// Multiple assertions (all failures shown)
Assert.Multiple(() => {                   → Assert(x == 5 && y == 10)
    Assert.That(x, Is.EqualTo(5));          // Native && shows all failures
    Assert.That(y, Is.EqualTo(10));
});
```

## FluentAssertions

Required imports:
```csharp
using static SharpAssert.Sharp;          // For Assert(), Throws<T>()
using SharpAssert;                        // For custom expectations (IsEquivalentTo, etc.)
using SharpAssert.Features.Collections;  // For IsInAscendingOrder(), AllUnique()
using SharpAssert.Features.Strings;      // For Matches(), occurrence counting
using SharpAssert.Features.Proximity;    // For BeCloseTo(), BeApproximately()
```

Migration:
```csharp
// Basic
actual.Should().Be(expected)              → Assert(actual == expected)
actual.Should().NotBe(expected)           → Assert(actual != expected)
actual.Should().BeTrue()                  → Assert(actual)
actual.Should().BeFalse()                 → Assert(!actual)
actual.Should().BeNull()                  → Assert(actual == null)
actual.Should().NotBeNull()               → Assert(actual != null)
actual.Should().BeGreaterThan(x)          → Assert(actual > x)
actual.Should().BeLessThan(x)             → Assert(actual < x)

// Strings
str.Should().StartWith(prefix)            → Assert(str.StartsWith(prefix))
str.Should().EndWith(suffix)              → Assert(str.EndsWith(suffix))
str.Should().Contain(substring)           → Assert(str.Contains(substring))
str.Should().Match(pattern)               → Assert(str.Matches(pattern))
str.Should().MatchEquivalentOf(pattern)   → Assert(str.MatchesIgnoringCase(pattern))
str.Should().Contain(sub, Exactly.Once()) → Assert(str.Contains(sub, Occur.Exactly(1)))
str.Should().Contain(sub, AtLeast.Twice())→ Assert(str.Contains(sub, Occur.AtLeast(2)))
str.Should().MatchRegex(pattern)          → Assert(str.MatchesRegex(pattern, Occur.AtLeast(1)))

// Collections
coll.Should().Contain(item)               → Assert(coll.Contains(item))
coll.Should().NotContain(item)            → Assert(!coll.Contains(item))
coll.Should().BeEmpty()                   → Assert(!coll.Any())
coll.Should().NotBeEmpty()                → Assert(coll.Any())
coll.Should().HaveCount(n)                → Assert(coll.Count() == n)
coll.Should().Equal(expected)             → Assert(coll.SequenceEqual(expected))
coll.Should().BeInAscendingOrder()        → Assert(coll.IsInAscendingOrder())
coll.Should().BeInDescendingOrder()       → Assert(coll.IsInDescendingOrder())
coll.Should().OnlyHaveUniqueItems()       → Assert(coll.AllUnique())
coll.Should().OnlyHaveUniqueItems(x=>x.K) → Assert(coll.AllUnique(x => x.K))
coll.Should().ContainSingle()             → Assert(coll.Count() == 1)
coll.Should().AllSatisfy(pred)            → Assert(coll.All(pred))
coll.Should().ContainInOrder(seq)         → Assert(coll.ContainsInOrder(seq))
coll.Should().ContainInConsecutiveOrder(s)→ Assert(coll.ContainsInConsecutiveOrder(s))
coll.Should().BeSubsetOf(superset)        → Assert(coll.IsSubsetOf(superset))
coll.Should().IntersectWith(other)        → Assert(coll.Intersects(other))
coll.Should().Satisfy(p1, p2, p3)         → Assert(coll.Satisfies(p1, p2, p3))

// Objects - Basic
obj.Should().BeEquivalentTo(exp)          → Assert(obj.IsEquivalentTo(exp))
obj.Should().BeEquivalentTo(exp, o=>o.Excluding(x=>x.Id))
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.Excluding(x=>x.Id)))
obj.Should().BeEquivalentTo(exp, o=>o.Including(x=>x.Name))
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.Including(x=>x.Name)))

// Objects - Collection Ordering
obj.Should().BeEquivalentTo(exp, o=>o.WithoutStrictOrdering())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.WithoutStrictOrdering()))
obj.Should().BeEquivalentTo(exp, o=>o.WithStrictOrdering())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.WithStrictOrdering()))

// Objects - Field Comparison
obj.Should().BeEquivalentTo(exp, o=>o.ExcludingFields())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.ExcludingFields()))
obj.Should().BeEquivalentTo(exp, o=>o.IncludingFields())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.IncludingFields()))

// Objects - Recursion Control
obj.Should().BeEquivalentTo(exp, o=>o.ExcludingNestedObjects())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.WithoutRecursing()))

// Objects - Custom Comparers
obj.Should().BeEquivalentTo(exp, o=>o.Using<T>((a,b) => a.Id == b.Id))
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.Using<T>((a,b) => a.Id == b.Id)))
obj.Should().BeEquivalentTo(exp, o=>o.Using<string>(StringComparer.OrdinalIgnoreCase))
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.Using(StringComparer.OrdinalIgnoreCase)))

// Objects - Value Semantics
obj.Should().BeEquivalentTo(exp, o=>o.ComparingByValue<Money>())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.ComparingByValue<Money>()))
obj.Should().BeEquivalentTo(exp, o=>o.ComparingEnumsByName())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.ComparingEnumsByName()))
obj.Should().BeEquivalentTo(exp, o=>o.ComparingRecordsByValue())
                                          → Assert(obj.IsEquivalentTo(exp, c=>c.ComparingRecordsByValue()))

// Note: ExcludingMissingMembers() is default behavior in SharpAssert

// Exceptions
act.Should().Throw<T>()                   → Assert(Throws<T>(act))
act.Should().NotThrow()                   → Assert(!Throws<Exception>(act))
act.Should().ThrowAsync<T>()              → Assert(await ThrowsAsync<T>(act))
act.Should().Throw<T>().WithMessage(msg)  → var ex = Throws<T>(act);
                                            Assert(ex.Message.Contains(msg))

// Multiple assertions (all failures shown)
using (new AssertionScope())              → Assert(x == 5 && y == 10)
{                                           // Native && shows all failures
    x.Should().Be(5);
    y.Should().Be(10);
}

// Numerics
val.Should().BePositive()                 → Assert(val > 0)
val.Should().BeNegative()                 → Assert(val < 0)
val.Should().BeInRange(min, max)          → Assert(val >= min && val <= max)
val.Should().BeCloseTo(target, precision) → Assert(val.BeCloseTo(target, precision))
                                          // Or native: Assert(Math.Abs(val - target) <= precision)
val.Should().BeApproximately(pi, 0.01)    → Assert(val.BeApproximately(pi, 0.01))
                                          // Or native: Assert(Math.Abs(val - pi) <= 0.01)

// DateTime Proximity
dt.Should().BeCloseTo(expected, 100.Milliseconds())
                                          → Assert(dt.BeCloseTo(expected, TimeSpan.FromMilliseconds(100)))
                                          // Or native: Assert(Math.Abs((dt - expected).TotalMilliseconds) <= 100)
dt.Should().BeAfter(baseDate).Within(5.Minutes())
                                          → Assert(dt >= baseDate && dt <= baseDate.AddMinutes(5))
dt.Should().BeBefore(deadline).Within(1.Hours())
                                          → Assert(dt <= deadline && dt >= deadline.AddHours(-1))
```
