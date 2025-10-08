# SharpAssert Demo Output

This document showcases all supported features of SharpAssert with detailed diagnostic output.

---

## 1. BASIC ASSERTIONS

> Fundamental assertion failures showing expression text and values

### Demo: Simple Failure
**Description:** Basic assertion failure with expression text

**Code:**
```csharp
Assert(false);
```

**Output:**
```
Assertion failed: false  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/01_BasicAssertionsDemos.cs:12
```

---

### Demo: Expression Text
**Description:** Shows operands and result for comparison expressions

**Code:**
```csharp
Assert(1 == 2);
```

**Output:**
```
Assertion failed: 1 == 2  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/01_BasicAssertionsDemos.cs:20
```

---

### Demo: Custom Message
**Description:** Assertion with custom failure message

**Code:**
```csharp
Assert(false, "This is a custom failure message");
```

**Output:**
```
This is a custom failure message
Assertion failed: false  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/01_BasicAssertionsDemos.cs:28
```

---

### Demo: Complex Expression
**Description:** Multi-variable expression with operators

**Code:**
```csharp
var x = 10;
var y = 5;
var z = 3;
Assert(x + y * z > 100);
```

**Output:**
```
Assertion failed: x + y * z > 100  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/01_BasicAssertionsDemos.cs:39
  Left:  25
  Right: 100
```

---

## 2. BINARY COMPARISONS

> Equality, inequality, and relational operator comparisons

### Demo: Equality Operators
**Description:** == and != showing left and right values

**Code:**
```csharp
var actual = 42;
var expected = 100;
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/02_BinaryComparisonDemos.cs:14
  Left:  42
  Right: 100
```

---

### Demo: Relational Operators
**Description:** <, <=, >, >= with numbers

**Code:**
```csharp
var value = 5;
var threshold = 10;
Assert(value > threshold);
```

**Output:**
```
Assertion failed: value > threshold  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/02_BinaryComparisonDemos.cs:24
  Left:  5
  Right: 10
```

---

### Demo: Null Comparisons
**Description:** null vs non-null showing both sides

**Code:**
```csharp
string? nullValue = null;
var nonNullValue = "text";
Assert(nullValue == nonNullValue);
```

**Output:**
```
Assertion failed: nullValue == nonNullValue  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/02_BinaryComparisonDemos.cs:34
  Left:  null
  Right: "text"
```

---

### Demo: Single Evaluation
**Description:** Method calls happen only once

**Code:**
```csharp
callCount = 0;
Assert(GetValue() == 100);
```

**Output:**
```
Assertion failed: GetValue() == 100  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/02_BinaryComparisonDemos.cs:50
  Left:  42
  Right: 100
```

---

### Demo: Type Mismatch
**Description:** Comparing different types

**Code:**
```csharp
object intValue = 42;
object stringValue = "42";
Assert(intValue.Equals(stringValue));
```

**Output:**
```
Assertion failed: intValue.Equals(stringValue)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/02_BinaryComparisonDemos.cs:60
```

---

## 3. LOGICAL OPERATORS

> AND, OR, NOT operators with short-circuit evaluation

### Demo: AND Failure
**Description:** Shows which operand failed

**Code:**
```csharp
var left = true;
var right = false;
Assert(left && right);
```

**Output:**
```
Assertion failed: left && right  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/03_LogicalOperatorDemos.cs:14
  Left:  True
  Right: False
  &&: Right operand was false
```

---

### Demo: Short-Circuit AND
**Description:** Right side not evaluated when left is false

**Code:**
```csharp
var condition = false;
Assert(condition && ThrowsException());
```

**Output:**
```
Assertion failed: condition && ThrowsException()  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/03_LogicalOperatorDemos.cs:23
  Left:  False (short-circuit)
  &&: Left operand was false
```

---

### Demo: OR Failure
**Description:** Both operands evaluated and shown

**Code:**
```csharp
var left = false;
var right = false;
Assert(left || right);
```

**Output:**
```
Assertion failed: left || right  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/03_LogicalOperatorDemos.cs:38
  Left:  False
  Right: False
  ||: Both operands were false
```

---

### Demo: NOT Operator
**Description:** Shows the actual value being negated

**Code:**
```csharp
var value = true;
Assert(!value);
```

**Output:**
```
Assertion failed: !value  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/03_LogicalOperatorDemos.cs:47
  Operand: True
  !: Operand was True
```

---

### Demo: Nested Logical
**Description:** Complex expressions with multiple operators

**Code:**
```csharp
var a = true;
var b = false;
var c = true;
var d = false;
Assert((a && b) || (c && d));
```

**Output:**
```
Assertion failed: (a && b) || (c && d)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/03_LogicalOperatorDemos.cs:59
  Left:  False
  Right: False
  ||: Both operands were false
```

---

## 4. STRING COMPARISONS

> Single-line and multiline string diffs with character-level highlighting

### Demo: Single-Line Diff
**Description:** Inline character diff for single-line strings

**Code:**
```csharp
var actual = "hello world";
var expected = "hallo world";
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/04_StringComparisonDemos.cs:14
  Left:  "hello world"
  Right: "hallo world"
  Diff: h[-e][+a]llo world
```

---

### Demo: Multiline Diff
**Description:** Line-by-line comparison for multiline strings

**Code:**
```csharp
var actual = """
    Line 1: Introduction
    Line 2: Body content
    Line 3: Conclusion
    """;
var expected = """
    Line 1: Introduction
    Line 2: Different content
    Line 3: Conclusion
    """;
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/04_StringComparisonDemos.cs:32
  Left:  "Line 1: Introduction
Line 2: Body content
Line 3: Conclusion"
  Right: "Line 1: Introduction
Line 2: Different content
Line 3: Conclusion"
  - Line 2: Body content
  + Line 2: Different content
```

---

### Demo: Null vs String
**Description:** null compared to non-null string

**Code:**
```csharp
string? nullString = null;
var nonNullString = "text";
Assert(nullString == nonNullString);
```

**Output:**
```
Assertion failed: nullString == nonNullString  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/04_StringComparisonDemos.cs:42
  Left:  null
  Right: "text"
```

---

### Demo: Empty vs Non-Empty
**Description:** Empty string vs text

**Code:**
```csharp
var empty = "";
var nonEmpty = "text";
Assert(empty == nonEmpty);
```

**Output:**
```
Assertion failed: empty == nonEmpty  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/04_StringComparisonDemos.cs:52
  Left:  ""
  Right: "text"
  Diff: [+text]
```

---

### Demo: Long Strings
**Description:** Long text with diff and truncation

**Code:**
```csharp
var actual = "The quick brown fox jumps over the lazy dog. This is a very long string that demonstrates how SharpAssert handles lengthy text comparisons with proper formatting and truncation when necessary.";
var expected = "The quick brown fox jumps over the lazy cat. This is a very long string that demonstrates how SharpAssert handles lengthy text comparisons with proper formatting and truncation when necessary.";
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/04_StringComparisonDemos.cs:62
  Left:  "The quick brown fox jumps over the lazy dog. This is a very long string that demonstrates how SharpAssert handles lengthy text comparisons with proper formatting and truncation when necessary."
  Right: "The quick brown fox jumps over the lazy cat. This is a very long string that demonstrates how SharpAssert handles lengthy text comparisons with proper formatting and truncation when necessary."
  Diff: The quick brown fox jumps over the lazy [-dog][+cat]. This is a very long string that demonstrates how SharpAssert handles lengthy text comparisons with proper formatting and truncation when necessary.
```

---

## 5. COLLECTION COMPARISONS

> Array and list comparisons showing differences, missing, and extra elements

### Demo: First Mismatch
**Description:** Shows first element that differs

**Code:**
```csharp
var actual = new[] { 1, 2, 3 };
var expected = new[] { 1, 2, 4 };
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/05_CollectionComparisonDemos.cs:14
  Left:  [1, 2, 3]
  Right: [1, 2, 4]
  First difference at index 2: expected 3, got 4
```

---

### Demo: Missing Elements
**Description:** Shows elements present in expected but not actual

**Code:**
```csharp
var actual = new[] { 1, 2 };
var expected = new[] { 1, 2, 3 };
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/05_CollectionComparisonDemos.cs:24
  Left:  [1, 2]
  Right: [1, 2, 3]
  Missing elements: [3]
```

---

### Demo: Extra Elements
**Description:** Shows elements in actual but not expected

**Code:**
```csharp
var actual = new[] { 1, 2, 3 };
var expected = new[] { 1, 2 };
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/05_CollectionComparisonDemos.cs:34
  Left:  [1, 2, 3]
  Right: [1, 2]
  Extra elements: [3]
```

---

### Demo: Empty Collection
**Description:** Empty vs non-empty collection

**Code:**
```csharp
var actual = Array.Empty<int>();
var expected = new[] { 1 };
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/05_CollectionComparisonDemos.cs:44
  Left:  []
  Right: [1]
  Missing elements: [1]
```

---

### Demo: Different Lengths
**Description:** Collections with different sizes

**Code:**
```csharp
var actual = new[] { 1, 2, 3, 4, 5 };
var expected = new[] { 1, 2 };
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/05_CollectionComparisonDemos.cs:54
  Left:  [1, 2, 3, 4, 5]
  Right: [1, 2]
  Extra elements: [3, 4, 5]
```

---

### Demo: Large Collections
**Description:** Preview truncation for large collections

**Code:**
```csharp
var actual = Enumerable.Range(1, 100).ToArray();
var expected = Enumerable.Range(1, 100).Select(x => x == 50 ? 999 : x).ToArray();
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/05_CollectionComparisonDemos.cs:64
  Left:  [1, 2, 3, 4, 5, 6, 7, 8, 9, ... (100 items)]
  Right: [1, 2, 3, 4, 5, 6, 7, 8, 9, ... (100 items)]
  First difference at index 49: expected 50, got 999
```

---

## 6. OBJECT COMPARISONS

> Deep object comparison showing property-level differences

### Demo: Property Difference
**Description:** Simple object with different property values

**Code:**
```csharp
var actual = new Person("Alice", 30, "New York");
var expected = new Person("Alice", 25, "New York");
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/06_ObjectComparisonDemos.cs:18
  Property differences:
    Age: expected '30', got '25'
```

---

### Demo: Nested Objects
**Description:** Deep property paths like Address.City

**Code:**
```csharp
var actual = new Customer(
    "Bob",
    new Address("123 Main St", "Boston", "02101"));
var expected = new Customer(
    "Bob",
    new Address("123 Main St", "New York", "10001"));
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/06_ObjectComparisonDemos.cs:32
  Property differences:
    Address.City: expected 'Boston', got 'New York'
    Address.ZipCode: expected '02101', got '10001'
```

---

### Demo: Null Object
**Description:** null vs object instance

**Code:**
```csharp
Person? nullPerson = null;
var nonNullPerson = new Person("Charlie", 35, "Chicago");
Assert(nullPerson == nonNullPerson);
```

**Output:**
```
Assertion failed: nullPerson == nonNullPerson  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/06_ObjectComparisonDemos.cs:42
  Left:  null
  Right: Person { Name = Charlie, Age = 35, City = Chicago }
```

---

### Demo: Record Comparison
**Description:** Record type value equality

**Code:**
```csharp
var actual = new Person("David", 40, "Denver");
var expected = new Person("David", 40, "Detroit");
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/06_ObjectComparisonDemos.cs:52
  Property differences:
    City: expected 'Denver', got 'Detroit'
```

---

### Demo: Multiple Differences
**Description:** Multiple properties differ

**Code:**
```csharp
var actual = new Person("Eve", 28, "Seattle");
var expected = new Person("Eva", 30, "Portland");
Assert(actual == expected);
```

**Output:**
```
Assertion failed: actual == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/06_ObjectComparisonDemos.cs:62
  Property differences:
    Name: expected 'Eve', got 'Eva'
    Age: expected '28', got '30'
    City: expected 'Seattle', got 'Portland'
```

---

## 7. LINQ OPERATIONS

> Contains, Any, All with predicates showing matching/failing items

### Demo: Contains Failure
**Description:** Shows collection contents when item not found

**Code:**
```csharp
var items = new[] { 1, 2, 3, 4, 5 };
var missingItem = 10;
Assert(items.Contains(missingItem));
```

**Output:**
```
Assertion failed: items.Contains(missingItem)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/07_LinqOperationDemos.cs:14
  Contains failed: searched for 10 in [1, 2, 3, 4, 5]
  Count: 5
```

---

### Demo: Any with Predicate
**Description:** Shows items matching predicate

**Code:**
```csharp
var items = new[] { 1, 2, 3, 4, 5 };
Assert(items.Any(x => x > 10));
```

**Output:**
```
Assertion failed: items.Any(x => x > 10)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/07_LinqOperationDemos.cs:23
  Any failed: no items matched x => (x > 10) in [1, 2, 3, 4, 5]
```

---

### Demo: All with Predicate
**Description:** Shows items failing predicate

**Code:**
```csharp
var items = new[] { 1, 2, 3, 4, 5 };
Assert(items.All(x => x > 3));
```

**Output:**
```
Assertion failed: items.All(x => x > 3)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/07_LinqOperationDemos.cs:32
  All failed: items [1, 2, 3] did not match x => (x > 3)
```

---

### Demo: Empty Collection
**Description:** LINQ on empty collection

**Code:**
```csharp
var items = Array.Empty<int>();
Assert(items.Any());
```

**Output:**
```
Assertion failed: items.Any()  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/07_LinqOperationDemos.cs:41
  Any failed: collection is empty
```

---

## 8. SEQUENCE EQUAL

> SequenceEqual with unified diff display

### Demo: Unified Diff
**Description:** Side-by-side sequence comparison

**Code:**
```csharp
var actual = new[] { 1, 2, 3, 4, 5 };
var expected = new[] { 1, 2, 9, 4, 5 };
Assert(actual.SequenceEqual(expected));
```

**Output:**
```
Assertion failed: actual.SequenceEqual(expected)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/08_SequenceEqualDemos.cs:14
  SequenceEqual failed: sequences differ
  Unified diff:
   [0] 1
   [1] 2
  -[2] 3
  +[2] 9
```

---

### Demo: Different Lengths
**Description:** Sequences of different sizes

**Code:**
```csharp
var actual = new[] { 1, 2, 3 };
var expected = new[] { 1, 2, 3, 4, 5 };
Assert(actual.SequenceEqual(expected));
```

**Output:**
```
Assertion failed: actual.SequenceEqual(expected)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/08_SequenceEqualDemos.cs:24
  SequenceEqual failed: length mismatch
  Expected length: 5
  Actual length:   3
  First:  [1, 2, 3]
  Second: [1, 2, 3, 4, 5]
```

---

### Demo: Element-by-Element
**Description:** Detailed element comparison

**Code:**
```csharp
var actual = new[] { "apple", "banana", "cherry" };
var expected = new[] { "apple", "orange", "cherry" };
Assert(actual.SequenceEqual(expected));
```

**Output:**
```
Assertion failed: actual.SequenceEqual(expected)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/08_SequenceEqualDemos.cs:34
  SequenceEqual failed: sequences differ
  Unified diff:
   [0] "apple"
  -[1] "banana"
  +[1] "orange"
```

---

### Demo: Large Sequences
**Description:** Truncation for large sequences

**Code:**
```csharp
var actual = Enumerable.Range(1, 50).ToArray();
var expected = Enumerable.Range(1, 50).Select(x => x == 25 ? 999 : x).ToArray();
Assert(actual.SequenceEqual(expected));
```

**Output:**
```
Assertion failed: actual.SequenceEqual(expected)  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/08_SequenceEqualDemos.cs:44
  SequenceEqual failed: sequences differ
  Unified diff:
   [21] 22
   [22] 23
   [23] 24
  -[24] 25
  +[24] 999
```

---

## 9. ASYNC OPERATIONS

> Async/await assertions showing awaited values

### Demo: Basic Await
**Description:** Simple async condition

**Code:**
```csharp
Assert(await GetBoolAsync());
```

**Output:**
```
Assertion failed: await GetBoolAsync()  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/09_AsyncDemos.cs:36
  Result: False
```

---

### Demo: Async Binary
**Description:** Both sides awaited with values shown

**Code:**
```csharp
Assert(await GetLeftValueAsync() == await GetRightValueAsync());
```

**Output:**
```
Assertion failed: await GetLeftValueAsync() == await GetRightValueAsync()  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/09_AsyncDemos.cs:44
  Left:  42
  Right: 100
```

---

### Demo: Mixed Async/Sync
**Description:** Await on one side, constant on other

**Code:**
```csharp
Assert(await GetLeftValueAsync() == 100);
```

**Output:**
```
Assertion failed: await GetLeftValueAsync() == 100  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/09_AsyncDemos.cs:52
  Left:  42
  Right: 100
```

---

### Demo: Async String Diff
**Description:** Awaited string with diff

**Code:**
```csharp
Assert(await GetStringAsync() == "expected value");
```

**Output:**
```
Assertion failed: await GetStringAsync() == "expected value"  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/09_AsyncDemos.cs:60
  Left:  "actual value"
  Right: "expected value"
  Diff: [-a][+expe]ct[-ual][+ed] value
```

---

## 10. DYNAMIC TYPES

> Dynamic type assertions with DLR evaluation

### Demo: Dynamic Binary
**Description:** Dynamic comparison showing values

**Code:**
```csharp
dynamic value = 42;
Assert(value == 100);
```

**Output:**
```
Assertion failed: value == 100  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/10_DynamicDemos.cs:13
  Left:  42
  Right: 100
```

---

### Demo: Dynamic Method Call
**Description:** Method call on dynamic object

**Code:**
```csharp
dynamic obj = new DynamicObject();
Assert(obj.GetValue() > 100);
```

**Output:**
```
Assertion failed: obj.GetValue() > 100  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/10_DynamicDemos.cs:22
  Left:  42
  Right: 100
```

---

### Demo: Dynamic Operators
**Description:** Dynamic operator semantics

**Code:**
```csharp
dynamic left = 10;
dynamic right = 20;
Assert(left > right);
```

**Output:**
```
Assertion failed: left > right  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/10_DynamicDemos.cs:32
  Left:  10
  Right: 20
```

---

## 11. NULLABLE TYPES

> Nullable value types and reference types showing HasValue and Value

### Demo: Nullable Int (null)
**Description:** int? null showing HasValue: false

**Code:**
```csharp
int? value = null;
Assert(value == 42);
```

**Output:**
```
Assertion failed: value == 42  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/11_NullableTypeDemos.cs:13
  Left:  HasValue: false
  Right: HasValue: true, Value: 42
```

---

### Demo: Nullable Int (value)
**Description:** int? with value showing HasValue: true, Value

**Code:**
```csharp
int? value = 42;
Assert(value == 100);
```

**Output:**
```
Assertion failed: value == 100  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/11_NullableTypeDemos.cs:22
  Left:  HasValue: true, Value: 42
  Right: HasValue: true, Value: 100
```

---

### Demo: Nullable Bool
**Description:** bool? comparison

**Code:**
```csharp
bool? value = false;
Assert(value == true);
```

**Output:**
```
Assertion failed: value == true  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/11_NullableTypeDemos.cs:31
  Left:  HasValue: true, Value: False
  Right: HasValue: true, Value: True
```

---

### Demo: Nullable DateTime
**Description:** DateTime? null comparison

**Code:**
```csharp
DateTime? value = null;
var expected = DateTime.Now;
Assert(value == expected);
```

**Output:**
```
Assertion failed: value == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/11_NullableTypeDemos.cs:41
  Left:  HasValue: false
  Right: HasValue: true, Value: 10/8/2025
```

---

### Demo: Nullable Reference Types
**Description:** string?, object? comparisons

**Code:**
```csharp
string? value = null;
string expected = "text";
Assert(value == expected);
```

**Output:**
```
Assertion failed: value == expected  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/11_NullableTypeDemos.cs:51
  Left:  null
  Right: "text"
```

---

### Demo: Null Comparison Edge Cases
**Description:** nullable == null

**Code:**
```csharp
int? value = 42;
Assert(value == null);
```

**Output:**
```
Assertion failed: value == null  at /Users/yb/work/oss/SharpAssert/src/SharpAssert.Demo/Demos/11_NullableTypeDemos.cs:60
  Left:  HasValue: true, Value: 42
  Right: HasValue: false
```

---

## Summary

**Total categories demonstrated:** 11
**Total demo cases:** 51

All assertions intentionally failed to showcase SharpAssert's rich diagnostic output.

SharpAssert provides pytest-style assertions with detailed failure messages, helping you understand exactly why your tests fail.
