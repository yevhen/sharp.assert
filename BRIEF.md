# SharpAssert: Write Assertions, Not Riddles

Tired of deciphering NUnit's `Is.EqualTo(x)` or chaining FluentAssertions like `Should().BeEquivalentTo()`? SharpAssert 
transforms your tests by letting you use plain, intuitive C# for your assertions. It eliminates domain-specific languages (DSLs) 
for testing and gives you incredibly detailed error messages when things go wrong.

## The SharpAssert Philosophy: Just Write C#

The core idea is simple: **an assertion is just a boolean expression**. If it's `true`, the test passes. If it's `false`, it fails. 
SharpAssert hooks into the build process to automatically rewrite your simple boolean checks into rich, diagnostic assertions.

**The Old Way:**
```csharp
// NUnit
Assert.That(result, Is.EqualTo(expected));

// FluentAssertions
result.Should().Be(expected);
```

**The Clean Way:**
```csharp
Assert(result == expected);
```

When this fails, SharpAssert provides a rich diagnostic message, showing the value of each part of the expression:
```
Assertion failed: result == expected at MyTest.cs:15
Left:  4
Right: 5
Result: false
```

Or consider this simple test:
```csharp
var items = new[] { 1, 2, 3 };
var target = 4;

Assert(items.Contains(target));
```

When this fails you'll get the rich diagnostics:
```
Assertion failed: items.Contains(target) at ItemTests.cs:211
items:  [1, 2, 3]
target: 4
Result: false
```

This level of detail makes debugging failed tests trivial.
The result is cleaner, more readable test code that leverages the C# syntax you already know and love,
removing the need to learn and apply, and decipher a separate assertion DSL.

## Side-by-Side: Common Assertion Idioms

Let's see how common NUnit and FluentAssertions patterns are simplified with SharpAssert, using real examples from the migration.

### 1. Equality and Binary Comparisons

No more `Is.EqualTo` or `Should().Be()`. Just use standard C# operators.

| Scenario | NUnit / FluentAssertions | SharpAssert |
| :--- | :--- | :--- |
| **Simple Equality** | `Assert.That(count, Is.EqualTo(3));`<br>`count.Should().Be(3);` | `Assert(count == 3);` |
| **Property Check** | `Assert.That(item.Data, Is.EqualTo("foo"));`<br>`item.Data.Should().Be("foo");` | `Assert(item.Data == "foo");` |
| **Inequality** | `Assert.That(a, Is.Not.EqualTo(b));`<br>`a.Should().NotBe(b);` | `Assert(a != b);` |

### 2. Boolean Checks

Forget `Is.True` and `Should().BeTrue()`. Your assertions are already boolean!

| Scenario | NUnit / FluentAssertions | SharpAssert |
| :--- | :--- | :--- |
| **Assert True** | `Assert.True(executed);`<br>`executed.Should().BeTrue();` | `Assert(executed);` |
| **Assert False** | `Assert.False(nextPipeExecuted);`<br>`nextPipeExecuted.Should().BeFalse();` | `Assert(!nextPipeExecuted);` |

### 3. Collection and Sequence Comparisons

This is where SharpAssert truly shines by simplifying verbose collection assertions and using familiar LINQ-style methods.

| Scenario | NUnit / FluentAssertions | SharpAssert |
| :--- | :--- | :--- |
| **Sequence Equality** | `Assert.That(batch, Is.EqualTo(new[] {"i1", "i2"}));`<br>`batch.Should().Equal(new[] {"i1", "i2"});` | `Assert(batch.SequenceEqual(new[] {"i1", "i2"}));` |
| **Collection Contains** | `Assert.That(nextReceived, Does.Contain(42));`<br>`nextReceived.Should().Contain(42);` | `Assert(nextReceived.Contains(42));` |
| **Collection Empty** | `Assert.That(mainProcessed, Is.Empty);`<br>`mainProcessed.Should().BeEmpty();` | `Assert(!mainProcessed.Any());` |
| **Collection Count** | `Assert.That(items, Has.Count.EqualTo(2));`<br>`items.Should().HaveCount(2);` | `Assert(items.Count == 2);` |

### 4. Object Identity (Reference Equality)

Checking if two variables point to the same object becomes more explicit and clear using the standard `ReferenceEquals` method.

| Scenario | NUnit / FluentAssertions | SharpAssert |
| :--- | :--- | :--- |
| **Reference Equality** | `Assert.That(doneItems[0], Is.SameAs(item));`<br>`doneItems[0].Should().BeSameAs(item);` | `Assert(ReferenceEquals(doneItems[0], item));` |

### 5. Exception Assertions

SharpAssert provides a powerful and composable way to test for exceptions that feels more integrated with the C# language.

| Scenario | NUnit / FluentAssertions                                                                                                                             | SharpAssert |
| :--- |:-----------------------------------------------------------------------------------------------------------------------------------------------------| :--- |
| **Simple Throw** | `await Assert.Throws<InvalidCastException>(() => ...);`<br>`Action act = () => ...; act.Should().Throw<InvalidCastException>();`                     | `Assert(Throws<InvalidCastException>(() => ...));` |
| **Async Throw** | `await Assert.ThrowsAsync<ArgumentException>(() => ...);`<br>`Func<Task> act = async () => ...; await act.Should().ThrowAsync<ArgumentException>();` | `Assert(await ThrowsAsync<ArgumentException>(() => ...));` |
| **Check Exception Message** | `var ex = Assert.Throws<Exception>(...); Assert.That(ex.Message, ...);`<br>`act.Should().Throw<Exception>().WithMessage("...");`                     | `Assert(Throws<Exception>(...).Message == "...");` |

## Summary

By embracing standard C# syntax, SharpAssert offers a more direct, readable, and ultimately more maintainable way
to write tests. Instead of framework-specific jargon the language-native expressions could be used.
The result is code that is easier to write, easier to read, and comes with powerful diagnostics out-of-the-box.