# Changelog

## Unreleased

- **Breaking change:** Restored C# short-circuit evaluation for assertion `&&` operators, including async, dynamic, local-function, and pattern expressions. Conditions after the first false operand no longer run. Null and type guards keep their native behavior.
- Simplified compound failure messages to show the condition that stopped an AND expression or the failed alternatives of an OR expression. Removed repeated expression prefixes and logical `Left`/`Right` labels. Kept diagnostic values and logical groups without running conditions again.

## 1.6.1

- Fixed ambiguous conversions from `ExceptionResult<Exception>`, including awaited `ThrowsAsync<Exception>`. Specific and base exception assignments remain supported. (#4)
- Bounded NUnit's appended assertion result to `Passed` or `Failed`. Structured result access and formatted failure diagnostics remain available. (#4)

## Earlier changes

- Changed default `DateTimeOffset` equivalency to compare instants with tick precision. Use `Using<DateTimeOffset>((left, right) => left.EqualsExact(right))` to compare offsets too. (#3)
- Restored evaluation of all independent `&&` checks without diagnostic replay. Kept `||` short-circuit evaluation and failure-only diagnostic construction. (#3)
- Added native C# property patterns without expression trees or a fluent API. Root pattern failures report the source pattern and input type without reading properties again. (#3)
- Preserved source headers, nullable directives, and original compiler diagnostic line numbers during rewriting. (#3)
- Added nullable equivalency selectors and kept the existing non-generic CLR signatures. (#3)
- Added assertion context to missing or wrong exceptions in inline synchronous and asynchronous checks. Actions execute once. (#3)

- Removed the System.Drawing.Common dependency from object comparison to prevent version conflicts in non-Windows consumers. The comparison engine now uses SharpAssert.CompareNETObjects without font comparison.

- Accelerated successful assertions for supported comparisons, logical expressions, and method calls by deferring diagnostic analysis until failure.
- Captured operand values during the original evaluation to retain evaluation order and avoid calling side-effectful expressions again when building failure diagnostics.
- Removed shared expression-analysis state that could corrupt diagnostics during parallel assertions.
- Recorded the first failed item for `Enumerable.All` and a bounded failure preview for `Enumerable.Any` without a second enumeration.
- Avoided replaying custom `IEnumerable` sources in failed `Contains` and `SequenceEqual` diagnostics; their collection preview or diff is omitted when it cannot be obtained safely.
