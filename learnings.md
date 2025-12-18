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
- **Binary Comparison Message:** For failed binary comparisons, the message is augmented with the evaluated operands:
  Left: {value}
  Right: {value}
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
- **Overload Gotcha:** Because the rewriter builds a semantic model per-file (single syntax tree), adding overloads to `Sharp.Assert(...)` can make binding ambiguous for expressions that reference symbols from other files and cause those calls to stop rewriting; prefer a distinct entry point name (e.g., `AssertThat`) for non-bool asserts.
- **Single Assert Alternative:** A single `Assert(AssertValue ...)` entry point avoids overload ambiguity; the rewriter can always rewrite to `SharpInternal.AssertValue(Expression<Func<AssertValue>> ...)` and route at runtime based on which `op_Implicit` conversion the expression tree uses.
- **Conversion Gotchas:** C# forbids user-defined conversions to/from interfaces (so `AssertValue` cannot convert from `IExpectation`), and only allows one user-defined conversion in a chain (so types with `implicit operator bool` may need a direct conversion to `AssertValue` unless they inherit `Expectation`).
- **Avoid bool Conversions on Expectations:** Keeping `Throws<T>` results purely as `Expectation` (no `implicit operator bool`) keeps `!Throws(...)` and composition (`.Not()/.And()/.Or()`) on the expectation path without ExpressionAnalyzer special casing.
- **ExprNode for Method Calls:** For invocation expressions, capturing the receiver (e.g., as `ExprNode.Left`) in addition to arguments enables expectation composition to label operands (receiver vs argument) accurately.
- **Expectation Operators:** Supporting `&`/`|` for `Expectation` composition requires ExprNode generation for `&`/`|` and composition evaluators that understand `ExprNode.Left`/`Right`.
- **Async Rewrite Discrimination:** `Assert(await ...)` must still rewrite to `SharpInternal.AssertAsync` for awaited booleans, but `Assert(await ThrowsAsync(...))` must NOT be rewritten (expression trees can't contain await and `AssertAsync` expects `Task<bool>`); discriminating by awaiting `Task<bool>`/`ValueTask<bool>` keeps both working.
- **Expectation Ergonomics:** Prefer extension methods that construct expectations (e.g., `4.IsEven()`) and suffix expectation types with `Expectation` for clean call sites.
- **Two Construction Styles:** Provide both `using static` factories (great for unary: `Assert(IsEven(4))`) and extension methods (great for binary/parameterized: `Assert(actual.IsEquivalentTo(expected))`).
- **Record Inheritance Gotcha:** `record` types can only inherit from `object` or another `record`, so `ExceptionResult<T>` cannot be a record if it must inherit the `Expectation` base class.
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
- **DateTime Culture-Dependent Formatting:** DateTime.ToString() produces different output across platforms (macOS: "1/1/2023", Linux: "01/01/2023 00:00:00") - use `dt.ToString("M/d/yyyy", CultureInfo.InvariantCulture)` for consistent cross-platform formatting in error messages
- **DynamicInvoke Cross-Platform Issues:** Using `Expression.Lambda(expression).Compile().DynamicInvoke()` can fail on CI/certain runtimes with `NotSupportedException: Specified method is not supported` - use strongly-typed `Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()()` for direct invocation without reflection
- **Expression.Compile InvalidProgramException:** Expression compilation can emit invalid IL on some runtimes; prefer `Compile(preferInterpretation: true)` or catch `InvalidProgramException` and recompile in interpreted mode to ensure value extraction works on CI
- **ByRef-like expression evaluation:** Ref structs (e.g., `Span<T>`, `ReadOnlySpan<T>`) cannot be boxed to `object`; convert them to arrays (e.g., `MemoryExtensions.ToArray`) before value extraction to avoid interpreter `TypeLoadException`/`ArgumentException`.
- **Reflection resilience:** Do not assume specific overloads exist across runtimes; when reflecting for helpers like `MemoryExtensions.ToArray`, select via tolerant predicate and allow null fallback to avoid type initializer failures.
- **Compilation fallback breadth:** Expression compilation can also fail with `TypeLoadException`/`ArgumentException` on byref-like conversions; wrap compilation and fall back to interpreted mode or a null-returning delegate when both paths fail.
- **Interpretation-first evaluation:** Prefer `Lambda.Compile(preferInterpretation: true)` for expression value extraction to avoid invalid IL generation issues; only return null when both interpreted compilation and span-to-array normalization fail.
- **Evaluation sentinels:** When evaluation fails, return a recognizable sentinel (e.g., `EvaluationUnavailable`) and have formatters render `<unavailable: reason>` instead of null/incorrect data to keep diagnostics honest.
- **NUnit Assert.ThrowsAsync return type:** `Assert.ThrowsAsync<T>()` returns the exception instance, not a `Task`, so async test methods need an explicit awaitable (e.g., `action.Should().ThrowAsync<T>()`) to avoid CS1998 warnings.

## String Diffing Implementation (Increment 5)

- **DiffPlex Character-Level Diffs:** Use `Differ.CreateCharacterDiffs()` for single-line strings to show precise character changes like `h[-e][+a]llo`
- **DiffPlex Line-Level Diffs:** Use `Differ.CreateLineDiffs()` for multiline strings to show `- line2` and `+ MODIFIED` format
- **String vs Object Detection:** Check `leftValue is string && rightValue is string` for pure string-string comparisons, plus handle mixed null cases
- **Test-Driven Approach:** Write failing tests first, verify they fail for right reasons (PowerAssert fallback), then implement feature and update test expectations
- **FluentAssertions Wildcard Matching:** Use `*pattern*` in AssertExpressionThrows expectations to match partial message content flexibly

## Collection Comparison Implementation (Increment 6)

- **IEnumerable Detection:** Use `IsEnumerable(value) => value is IEnumerable && value is not string` to identify collections while excluding strings
- **Materialization Strategy:** Convert IEnumerable to `List<object?>` once to avoid re-enumeration issues when analyzing differences
- **First Difference Algorithm:** Use linear scan with index tracking to find first non-equal elements for precise error location
- **Missing/Extra Elements Detection:** Compare collection lengths and use `Skip()` + `Take()` for efficient subset extraction
- **Collection Preview Formatting:** Limit preview to first N elements with "... (X items)" suffix for large collections
- **List<T> Reference Equality:** List<T> uses reference equality by default, making `list1 == list2` perfect for testing collection formatter triggering
- **Expression Type Verification:** Collections trigger BinaryExpression with NodeType.Equal, confirming proper expression tree analysis path
- **Value Formatting Strategy:** Use pattern matching (`null => "null"`, `string s => $"\"{s}\""`, `_ => value.ToString()!`) for consistent display

## Test Pattern Consistency & TestBase Utilities

- **TestBase Utility Methods:** All test fixtures should inherit from TestBase and use the provided utility methods for consistency
  - `AssertExpressionThrows<T>()` - For testing expected exception scenarios with message pattern matching
  - `AssertExpressionDoesNotThrow()` - For testing successful assertion scenarios
- **CollectionComparisonFixture Inconsistency:** Originally called `SharpInternal.Assert()` directly instead of using TestBase utilities, breaking the established pattern used by other fixtures like LogicalOperatorFixture, BinaryComparisonFixture, StringComparisonFixture
- **Expression Tree Pattern:** All test fixtures should use `Expression<Func<bool>> expr = () => condition;` pattern for proper expression tree creation in tests
- **Parameter Order Awareness:** SharpInternal.Assert signature: (condition, expr, file, line, message=null, usePowerAssert=false) - must pass parameters in correct order

## Object Comparison Implementation (Increment 7)

- **CompareNETObjects Package:** Use package name `CompareNETObjects` (not `KellermanSoftware.CompareNETObjects`) and namespace `KellermanSoftware.CompareNetObjects`
- **CompareLogic Configuration:** Simple configuration approach `var logic = new CompareLogic(); logic.Config.MaxDifferences = MaxObjectDifferences;` is more reliable than complex object initializers
- **Object Detection Strategy:** Exclude strings (handled by StringComparisonFormatter), IEnumerable (handled by CollectionComparisonFormatter), primitives, and common value types from object comparison
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

## Async Support Implementation (Increment 10)

- **AssertAsync Method Design:** Create async method with signature `AssertAsync(Func<Task<bool>>, string, string, int)` for basic async support
- **Minimal Diagnostics Philosophy:** For async cases, provide basic failure information (expression text and "Result: False") rather than complex expression tree analysis
- **Exception Propagation Strategy:** Let async exceptions bubble up naturally - don't catch and wrap them unless necessary
- **BinaryOp Enum Addition:** Required for future async binary comparison support (Increment 11), defines comparison operators: Eq, Ne, Lt, Le, Gt, Ge
- **Test Migration Strategy:** Convert ignored placeholder tests to real async tests using FluentAssertions async assertion patterns (`await action.Should().NotThrowAsync()`)
- **Async Context Preservation:** AssertAsync naturally preserves SynchronizationContext through proper async/await usage - no special handling needed
- **Async Test Patterns:** Use `async Task` test methods with `await` for assertions, test both success and failure paths with appropriate exception expectations

## Async Binary Comparison Implementation (Increment 11)

- **Rewriter Architecture:** Async binary comparisons are handled at the rewriter level, since they are rewritten before becoming expression trees
- **Binary Operation Detection:** Use `IsBinaryOperation()` to detect comparison operators: ==, !=, <, <=, >, >= - requires proper parentheses grouping for operator precedence
- **SyntaxKind Token Names:** Use correct Roslyn token names: `LessThanEqualsToken` and `GreaterThanEqualsToken` (not the incorrect `LessThanOrEqualToken`)
- **Async Thunk Generation:** Generate thunks for both operands - `async () => operand` for await expressions, `() => Task.FromResult<object?>(operand)` for sync
- **Source Order Evaluation:** AssertAsyncBinary evaluates left operand first, then right operand (`await leftAsync(); await rightAsync()`) preserving source order as required
- **Formatter Reuse:** Async binary formatting reuses existing IComparisonFormatter infrastructure (StringComparisonFormatter, CollectionComparisonFormatter, etc.)
- **Type System Integration:** Cast nullable types properly with `SingletonSeparatedList<TypeSyntax>(objectType)` for SyntaxFactory TypeArgumentList
- **BinaryOp Enum Mapping:** Map SyntaxKind tokens to BinaryOp enum values for runtime evaluation: EqualsEqualsToken => "Eq", etc.
- **Test Strategy:** Use FluentAssertions `.Where(ex => ex.Message.Contains(...))` pattern for async exception testing with complex message validation

## Dynamic Support Implementation (Increment 12)

- **Dynamic Language Runtime (DLR) Integration:** Use `(dynamic?)leftValue == (dynamic?)rightValue` pattern for dynamic operator semantics in `EvaluateDynamicBinaryComparison`
- **Exception Handling Strategy:** Wrap dynamic operations in try-catch blocks and return false on failure - let comparison fail gracefully rather than propagate exceptions
- **Formatter Reuse:** Dynamic binary comparisons reuse existing `IComparisonFormatter` infrastructure (StringComparisonFormatter, CollectionComparisonFormatter, etc.) for consistent diff output
- **Minimal Diagnostics Philosophy:** For general dynamic expressions, provide basic failure information (expression text + "Result: False") following same pattern as async support
- **Method Signature Consistency:** Both `AssertDynamic` and `AssertDynamicBinary` follow same parameter pattern as async counterparts for consistency
- **Test Pattern Migration:** Convert ignored placeholder tests to real implementation tests using FluentAssertions `.Should().Throw<T>()` and `.Should().NotThrow()` patterns
- **Object Type Casting:** Dynamic thunks cast to `object?` for compatibility with comparison formatter system that expects object references

## Roslyn Syntax Trivia Patterns

- **Await Expression Construction:** Must use explicit token with spacing to avoid concatenation - `SyntaxFactory.Token(TriviaList(), SyntaxKind.AwaitKeyword, TriviaList(Space))` prevents `awaitglobal::` bug
- **Line Directive Placement:** Preprocessor directives must appear as first non-whitespace on line - attach to outer expression (AwaitExpression) not inner (InvocationExpression)
- **Trivia Attachment Strategy:** Use `WithLeadingTrivia()` and `WithTrailingTrivia()` on the outermost rewritten node to ensure directives wrap entire statement correctly
- **Spacing Between Tokens:** Always use `SyntaxFactory.Space` in trailing trivia of keywords when followed by identifiers/expressions
- **Return Type Changes:** Changing rewrite method return types (e.g., InvocationExpression to AwaitExpression) requires new trivia attachment methods to handle different node types
- **Line Directive Format:** Use `SyntaxFactory.PreprocessingMessage($"#line {lineNumber} \"{escapedPath}\"")` for file/line mapping, `PreprocessingMessage("#line default")` to reset
- **Trivia Ordering:** Leading trivia order: original trivia → line directive → newline; Trailing trivia order: newline → default directive → newline → original trivia
- **Expression Wrapping:** When wrapping expressions (e.g., adding await), construct inner expression first, then wrap with factory methods, then attach trivia to outer node

## Testing Infrastructure Refactoring

- **Decoupled Testing:** Separated logic verification (`LogicTests`) from formatting verification (`FormattingTests`) to reduce brittleness and improve maintenance.
- **Structural Verification:** Using `EvaluationResult` record hierarchy to verify assertion logic structurally via `AssertFails(action, expectedResult)` using `BeEquivalentTo` structural equality.
- **Rendering Verification:** Using `AssertRendersExactly(result, lines...)` to verify string output independent of logic execution.
- **DSL Helpers:** `TestBase` provides composable helpers like `BinaryComparison`, `Value`, `Operand` to construct expected results concisely.
- **Nested Fixtures:** Grouping tests by concern (`LogicTests` vs `FormattingTests`) keeps fixture files organized.
- **Result Records:** Rendering logic pushed down to data records (e.g. `InlineStringDiff.Render()`), making them self-rendering and composable.

## Collection Quantifier Expectations (Each)

- **Context Propagation:** Child context gets `[index]` appended to expression (e.g., `numbers.Each(...)[1]`) for precise failure location.
- **No Short-Circuit:** Evaluate ALL items to report ALL failures - complete diagnostics more useful than early exit.
- **Vacuous Truth:** Empty collections pass (no items means no failures possible).
- **Two Overloads Pattern:** `Func<T, Expectation>` for composed expectations, `Expression<Func<T, bool>>` for simple predicates.
- **PredicateExpectation Wrapper:** Internal class converts bool predicates to Expectation via `Expression.Body.ToString()` for diagnostic text.
- **Rendering Pattern:** `CollectionQuantifierResult` shows summary ("3 of 5 failed") then iterates failures with `[index]: BooleanValue` format.
- **AssertRendersExactly Gotcha:** Test helper ignores `IndentLevel` and just joins `Text` - test expectations shouldn't include leading spaces.
