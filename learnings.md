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
- **Fixtures:** Test fixtures are organized by functionality (e.g., `AssertionFixture`, `ExpressionAnalysisFixture`, `RewriterFixture`, `SharpLambdaRewriteTaskFixture`).
