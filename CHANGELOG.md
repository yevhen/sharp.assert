# Changelog

## Unreleased

- Removed the System.Drawing.Common dependency from object comparison to prevent version conflicts in non-Windows consumers. The comparison engine now uses SharpAssert.CompareNETObjects without font comparison.

- Accelerated successful assertions for supported comparisons, logical expressions, and method calls by deferring diagnostic analysis until failure.
- Captured operand values during the original evaluation to retain evaluation order and avoid calling side-effectful expressions again when building failure diagnostics.
- Removed shared expression-analysis state that could corrupt diagnostics during parallel assertions.
- Recorded the first failed item for `Enumerable.All` and a bounded failure preview for `Enumerable.Any` without a second enumeration.
- Avoided replaying custom `IEnumerable` sources in failed `Contains` and `SequenceEqual` diagnostics; their collection preview or diff is omitted when it cannot be obtained safely.
