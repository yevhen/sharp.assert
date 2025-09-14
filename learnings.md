# SharpAssert Development Learnings

This document is organized by topic to consolidate key learnings about the project's architecture, implementation, and testing.

## Project & Solution Structure

- **Namespaces:** The main library code resides in the `SharpAssert` namespace, but the public `Sharp.Assert` API is exposed in the global scope for ease of use. The custom `SharpAssertionException` lives within the `SharpAssert` namespace.
- **Test Project:** The test project (`SharpAssert.Tests`) should share the same `RootNamespace` as the main project (`SharpAssert`) but exist in its own separate physical namespace to mirror the structure of the code under test.
- **File Organization:**
    - `/SharpAssert/Sharp.cs`: Public API entry point.
    - `/SharpAssert/SharpAssertionException.cs`: Custom exception.
    - `/SharpAssert/SharpInternal.cs`: Internal API for rewriter targets.
    - `/SharpAssert/ExpressionAnalyzer.cs`: Core expression tree visitor.
    - `/SharpAssert.Tests/`: Contains all test fixtures.
- **Build Configuration:**
    - The project targets `net9.0` to leverage modern C# language features.
    - Careful attention is needed for compatibility with features like file-scoped namespaces and implicit usings when working with the rewriter.
- **Integration Testing:**
    - SharpAssert.IntegrationTest should use ProjectReference instead of PackageReference for true integration testing
    - This ensures changes to local code are immediately reflected without requiring package publishing
    - Remove RestorePackagesPath and RestoreAdditionalProjectSources when using ProjectReference

## Runtime: Expression Tree Analysis

- **Single Evaluation Principle:** A core requirement is to evaluate each operand and sub-expression only once. This is achieved by using a visitor pattern (`ExpressionAnalyzer`) combined with a `ConcurrentDictionary` to cache the results of evaluated sub-expressions.
- **Expression Tree Limitations:** Expression trees cannot contain local functions. All logic must be implemented as instance or static methods.
- **Binary Comparisons (`==`, `!=`, `>`, etc.):**
    - These are analyzed to extract the final values of the left and right operands for inclusion in the failure message.
    - A `BinaryOp` enum is used to represent all C# comparison operators.
- **Logical Operators (`&&`, `||`, `!`):**
    - These require special handling separate from binary comparisons.
    - `&&` maps to `ExpressionType.AndAlso`, `||` to `ExpressionType.OrElse`, and `!` to `ExpressionType.Not`.
    - **Short-Circuiting:** The natural short-circuiting behavior of `&&` and `||` is preserved by evaluating the entire expression first and only analyzing the sub-expressions if the assertion fails. This avoids artificially enforcing evaluation rules.
    - The `!` operator is a `UnaryExpression` and requires its own handling path.

## Runtime: Diagnostics & Formatting

- **Standard Failure Message:** The exception format is inspired by pytest: `Assertion failed: {expr} at {file}:{line}`.
- **Binary Comparison Message:** For failed binary comparisons, the message is augmented with the evaluated operands: `
  Left: {value}
  Right: {value}`.
- **Logical Operator Message:** For failed logical operations, the message shows the truthiness of the operands to provide context (e.g., "Left operand was false" for a short-circuited `&&`).
- **Readability:** Private helper methods are used extensively to keep the formatting logic clean and maintainable.

## MSBuild Source Rewriter

- **Core Technology:** The rewriter is implemented using Roslyn's `CSharpSyntaxRewriter` to perform AST transformation.
- **Invocation Detection:** Simple and fast identifier matching (`identifier.Identifier.ValueText == "Assert"`) is sufficient to find `Sharp.Assert` calls, avoiding the need for a full semantic model resolution in many cases.
- **Lambda Creation:** `SyntaxFactory.ParenthesizedLambdaExpression()` is used to wrap the user's expression into a lambda (`() => ...`). The factory produces compact but functionally correct code.
- **Async Prevention:** The rewriter detects `await` expressions (`.DescendantNodes().OfType<AwaitExpressionSyntax>().Any()`) and skips rewriting them to avoid generating invalid expression trees.
- **MSBuild Integration:**
    - The rewrite task is injected `BeforeTargets="CoreCompile"` to ensure it runs before the compiler.
    - The output is written to a standard pattern: `$(IntermediateOutputPath)SharpRewritten\**\*.sharp.g.cs`.
- **Graceful Fallback:** This is a critical principle. The MSBuild task is designed to fail gracefully. If the rewriter encounters an error, it copies the original source file, ensuring that a rewriter bug **does not break the user's build**.
- **Line Directive Implementation:** 
    - Use `SyntaxFactory.PreprocessingMessage()` instead of `SyntaxFactory.LineDirectiveTrivia()` for proper formatting of #line directives
    - Only add #line directives when actual rewrites occur to preserve unchanged files
    - Track rewrite state with `HasRewrites` property to conditionally add file-level #line directive
    - #line directives enable proper stack traces and debugging in original source files
- **Line Number Tracking (Phase 4):**
    - `GetLineSpan(node.Span).StartLinePosition.Line + 1` correctly extracts line numbers from syntax nodes
    - Multi-line expressions correctly use the start line of the Assert call for line mapping
    - Line numbers flow through the entire rewriting pipeline via the 4th parameter to SharpInternal.Assert
    - #line directives around rewritten Assert calls ensure accurate debugging and stack traces

## API Design & Dependencies

- **Public API:** The user-facing API is minimal, relying on `[CallerArgumentExpression]`, `[CallerFilePath]`, and `[CallerLineNumber]` to capture the assertion context automatically. These attributes work seamlessly in .NET 9.0.
- **Dependencies:** `FluentAssertions` is used within the test suite (`SharpAssert.Tests`) for creating readable and maintainable test assertions.

## Testing Strategy

- **Unit Tests:** The core rewriter functionality is tested using "golden file" tests, comparing the output of the rewriter against a known-good rewritten source file.
- **Integration Tests:** End-to-end tests are run on a sample project where `EnableSharpLambdaRewrite` is set to `true`, verifying that the entire process (rewrite -> compile -> run -> fail) works as expected.
- **MSBuild Task Testing:** Direct unit testing of SharpLambdaRewriteTask provides fast, comprehensive coverage without MSBuild complexity. Key insights:
  - MSBuild tasks require a `BuildEngine` to log messages - use MockBuildEngine in tests
  - Test the task directly by setting properties and calling Execute() 
  - Verify file processing, error handling, and configuration properties
  - Use temp directories for isolated file operations in tests
- **MSBuild Task Enhancement (Phase 3):** Key improvements for production-ready MSBuild tasks:
  - Add [Output] properties for MSBuild to track generated files
  - Implement comprehensive generated file detection (AssemblyInfo.cs, GlobalUsings.g.cs, .designer., etc.)
  - Use MessageImportance.Low for diagnostic messages to respect MSBuild verbosity levels
  - Maintain backward compatibility - existing tests expect files without Assert calls to be skipped entirely
  - Provide detailed file mapping diagnostics for troubleshooting rewriter issues
- **Fixtures:** Test fixtures are organized by functionality (e.g., `AssertionFixture`, `ExpressionAnalysisFixture`, `RewriterFixture`, `SharpLambdaRewriteTaskFixture`).

## MSBuild Integration Testing Innovation

- **`ReferenceOutputAssembly="false"` Pattern:** Critical for MSBuild task testing - include rewriter project in build order without assembly reference pollution
- **Direct `.targets` Import Strategy:** Import `.targets` file directly instead of via NuGet package for true integration testing without packaging overhead
- **`SharpAssertRewriterPath` Override:** Custom property to point to local development build instead of packaged tools directory (`$(MSBuildThisFileDirectory)..\SharpAssert.Rewriter\bin\$(Configuration)\net9.0\`)
- **MSBuild Task Assembly Loading:** Tasks loaded via `AssemblyFile` in UsingTask are cached by MSBuild process - recompiling rewriter doesn't reload task without MSBuild restart
- **Generated File Directory Pattern:** `$(IntermediateOutputPath)SharpRewritten\` provides predictable output location separate from source files
- **Design-Time Build Exclusion:** Critical to exclude rewriter during `$(DesignTimeBuild)` and `$(BuildingForLiveUnitTesting)` to preserve IDE experience
- **File Pattern Exclusion Logic:** Complex but necessary exclusion patterns for generated files: `Microsoft.NET.Test.Sdk.Program.cs`, `*.AssemblyInfo.cs`, `*.GlobalUsings.g.cs`

## Package Testing Isolation Techniques

- **Separate Solution Files:** Main solution (`SharpAssert.sln`) vs package testing (`SharpAssert.PackageTesting.sln`) prevents dev workflow contamination
- **NuGet Source Mapping:** `packageSourceMapping` in `nuget.package-tests.config` ensures SharpAssert packages only come from local feed, preventing version conflicts
- **Package Cache Isolation:** Use `--packages ./test-packages` for isolated cache that doesn't pollute global NuGet cache
- **File Linking with MSBuild:** `<Compile Include="..\SharpAssert.IntegrationTests\**\*.cs" Link="..."/>` shares test files without duplication
- **Wildcard Version References:** `Version="1.0.0-dev*"` allows testing against latest local builds without hardcoding timestamps
- **Config File Isolation:** Separate `nuget.package-tests.config` with `<clear/>` prevents interference from user/machine level configs

## Package Architecture & Structure

- **Conditional Project/Package References:** Use `Condition="Exists('..\SharpAssert.Runtime\SharpAssert.csproj')"` to enable local development with ProjectReference while maintaining PackageReference for NuGet packaging
- **Dependency Order in Packaging:** When packages depend on each other, pack dependencies first then reference local feed: `--source local-feed --source https://api.nuget.org/v3/index.json`
- **Single Package Strategy:** Users prefer installing one main package (e.g., "SharpAssert") with transitive dependencies (e.g., "SharpAssert.Runtime") rather than multiple packages
- **Wildcard Version Matching:** `Version="1.0.0-dev*"` enables flexible local development while maintaining precise version control in production

## Development Process & Workflow Insights

- **Multi-Layer Testing Strategy Benefits:** Unit (fast dev) → Integration (MSBuild behavior) → Package (real-world usage) → CI (clean environment)
- **Timestamp-Based Dev Versions:** `1.0.0-dev20250812155111` pattern enables rapid iteration without version conflicts
- **Cache Management Critical:** NuGet cache pollution is a major source of "works on my machine" issues - isolated caches prevent false positives
- **MSBuild vs Package Testing Trade-offs:** Integration tests faster but miss packaging issues; package tests slower but catch real deployment problems
- **Script-Based Automation:** `./dev-test.sh` (fast dev cycle) vs `./test-local.sh` (full validation) provides appropriate tool for each workflow stage

## Technical Gotchas & Problems Solved

- **MSBuild Property Evaluation Order:** Properties must be defined before UsingTask - `SharpAssertRewriterPath` must have default before being used in AssemblyFile
- **NuGet Package Source Precedence:** Without source mapping, higher priority sources can override local packages even with version wildcards
- **Project.Assets.Json Staleness:** Incremental MSBuild restore doesn't always detect new packages with same version pattern - requires `--force-evaluate`
- **Assembly Loading Context:** MSBuild tasks run in separate AppDomain - assembly conflicts between task dependencies and target project dependencies
- **Generated File Cleanup:** `dotnet clean` doesn't automatically remove custom output directories - requires explicit `<RemoveDir>` target with `BeforeTargets="Clean"`
- **MSBuild Incremental Builds:** Custom targets without `Inputs`/`Outputs` attributes run on every build - proper tracking requires moving ItemGroups outside target scope for evaluation
- **Cross-Platform Path Handling:** MSBuild path handling differences between Windows/Unix require careful attention to separators and absolute vs relative paths
- **Package Source Discovery:** NuGet source discovery during package testing can fail silently if local feed structure is incorrect - verify with `--verbosity detailed`

## String Diffing Implementation (Increment 5)

- **DiffPlex Character-Level Diffs:** Use `Differ.CreateCharacterDiffs()` for single-line strings to show precise character changes like `h[-e][+a]llo`
- **DiffPlex Line-Level Diffs:** Use `Differ.CreateLineDiffs()` for multiline strings to show `- line2` and `+ MODIFIED` format
- **String vs Object Detection:** Check `leftValue is string && rightValue is string` for pure string-string comparisons, plus handle mixed null cases
- **UnsupportedFeatureDetector Updates:** Remove implemented features from unsupported list - string comparisons no longer fall back to PowerAssert
- **Test-Driven Approach:** Write failing tests first, verify they fail for right reasons (PowerAssert fallback), then implement feature and update test expectations
- **FluentAssertions Wildcard Matching:** Use `*pattern*` in AssertExpressionThrows expectations to match partial message content flexibly

## Collection Comparison Implementation (Increment 6)

- **IEnumerable Detection:** Use `IsEnumerable(value) => value is IEnumerable && value is not string` to identify collections while excluding strings
- **Materialization Strategy:** Convert IEnumerable to `List<object?>` once to avoid re-enumeration issues when analyzing differences
- **First Difference Algorithm:** Use linear scan with index tracking to find first non-equal elements for precise error location
- **Missing/Extra Elements Detection:** Compare collection lengths and use `Skip()` + `Take()` for efficient subset extraction
- **Collection Preview Formatting:** Limit preview to first N elements with "... (X items)" suffix for large collections
- **Test Isolation with usePowerAssertForUnsupported=false:** Essential for testing SharpAssert formatters directly without PowerAssert fallback
- **List<T> Reference Equality:** List<T> uses reference equality by default, making `list1 == list2` perfect for testing collection formatter triggering
- **Expression Type Verification:** Collections trigger BinaryExpression with NodeType.Equal, confirming proper expression tree analysis path
- **Value Formatting Strategy:** Use pattern matching (`null => "null"`, `string s => $"\"{s}\""`, `_ => value.ToString()!`) for consistent display

## Test Pattern Consistency & TestBase Utilities

- **TestBase Utility Methods:** All test fixtures should inherit from TestBase and use the provided utility methods for consistency
  - `AssertExpressionThrows<T>()` - For testing expected exception scenarios with message pattern matching
  - `AssertExpressionDoesNotThrow()` - For testing successful assertion scenarios
- **CollectionComparisonFixture Inconsistency:** Originally called `SharpInternal.Assert()` directly instead of using TestBase utilities, breaking the established pattern used by other fixtures like LogicalOperatorFixture, BinaryComparisonFixture, StringComparisonFixture
- **PowerAssert Parameter Handling:** Extended TestBase with overload to support `usePowerAssertForUnsupported` parameter needed for testing specific SharpAssert features without PowerAssert fallback
- **Expression Tree Pattern:** All test fixtures should use `Expression<Func<bool>> expr = () => condition;` pattern for proper expression tree creation in tests
- **Parameter Order Awareness:** SharpInternal.Assert signature: (condition, expr, file, line, message=null, usePowerAssert=false, usePowerAssertForUnsupported=true) - must pass parameters in correct order

## CRITICAL: UnsupportedFeatureDetector Maintenance

- **Feature Implementation Dependencies:** When implementing support for new comparison types (strings, collections, objects, LINQ), MUST update UnsupportedFeatureDetector to remove the feature from unsupported list
- **Common Bug Pattern:** Implementing formatter (e.g., CollectionComparisonFormatter) but forgetting to update UnsupportedFeatureDetector causes feature to incorrectly fall back to PowerAssert instead of using new formatter
- **Fix Strategy:** Remove detection logic from UnsupportedFeatureDetector.Visit* methods when corresponding IComparisonFormatter is implemented and registered
- **Test Impact:** Update related UnsupportedFeatureDetectorFixture tests to expect features as supported rather than unsupported
- **Root Cause Prevention:** Always check UnsupportedFeatureDetector when implementing new comparison formatters - it's the gatekeeper that determines PowerAssert vs SharpAssert routing

## Object Comparison Implementation (Increment 7)

- **CompareNETObjects Package:** Use package name `CompareNETObjects` (not `KellermanSoftware.CompareNETObjects`) and namespace `KellermanSoftware.CompareNetObjects`
- **CompareLogic Configuration:** Simple configuration approach `var logic = new CompareLogic(); logic.Config.MaxDifferences = MaxObjectDifferences;` is more reliable than complex object initializers
- **Object Detection Strategy:** Exclude strings (handled by StringComparisonFormatter), IEnumerable (handled by CollectionComparisonFormatter), primitives, and common value types from object comparison
- **Never Considered Unsupported:** Object comparisons were always supported via BinaryExpression path, they just used DefaultComparisonFormatter before - no UnsupportedFeatureDetector changes needed
- **Property Path Formatting:** CompareNETObjects provides property paths like "Address.City" automatically - no need to manually construct nested paths
- **Test Pattern Consistency:** All ObjectComparisonFixture tests should use the same `Expression<Func<bool>> expr = () => condition;` pattern with AssertExpressionThrows/DoesNotThrow from TestBase
- **Configuration Constants:** Use hardcoded constants like `MaxObjectDifferences = 20` following same pattern as StringDiffer, rather than premature configuration system integration
- **Custom Equals Handling:** C# `==` operator naturally respects overridden Equals methods - no special handling needed in formatter, just let binary comparison work normally

## SequenceEqual Implementation (Increment 9)

- **DiffPlex Import Requirements:** Need both `using DiffPlex.Model;` and the qualified `DiffResult` type for proper compilation
- **SequenceEqual Method Detection:** Added to ExpressionAnalyzer MethodCallExpression handling alongside Contains/Any/All for LINQ operations
- **Unified Diff Strategy:** Use DiffPlex `Differ.CreateLineDiffs()` for sequence comparison, treating each element as a "line"
- **Materialization Pattern:** Convert IEnumerable to List<object?> once to avoid multiple enumeration issues during analysis
- **Length Mismatch Handling:** Special case formatting when sequences have different lengths vs different content
- **Custom Comparer Detection:** Check `methodCall.Arguments.Count` to detect when custom IEqualityComparer is provided
- **Truncation Logic:** Apply MaxDiffLines limit to unified diff output with "truncated" message for large diffs
- **Static Extension Support:** Works with both instance method syntax (`seq1.SequenceEqual(seq2)`) and static syntax (`Enumerable.SequenceEqual(seq1, seq2)`)
- **Test Structure Consistency:** Used same pattern as LinqOperationsFixture with nested TestFixture classes (PositiveTestCases, FailureFormatting, StaticExtensionMethods)
- **UnsupportedFeatureDetector Maintenance:** Critical to update test expectations when features move from unsupported to supported status