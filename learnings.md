# SharpAssert Development Learnings

## Foundation Implementation (Increment 1)

### Key Discoveries
- CallerArgumentExpression, CallerFilePath, and CallerLineNumber work seamlessly in .NET 9.0 without additional configuration
- FluentAssertions integrates perfectly with NUnit 4.2.2 for readable test assertions
- The namespace structure requires careful attention - main library uses `SharpAssert` namespace, public API is in global scope
- Test project should have same RootNamespace as main project but be in separate physical namespace

### Technical Insights
- Exception formatting follows pytest-style: "Assertion failed: {expr} at {file}:{line}"
- Private helper methods improve readability for message formatting
- Using statements can reference both namespaces (SharpAssert for exceptions) and static classes (Sharp for API)

### Project Structure
- `/SharpAssert/Sharp.cs` - Public API entry point (global scope)
- `/SharpAssert/SharpAssertionException.cs` - Custom exception (SharpAssert namespace)
- `/SharpAssert.Tests/AssertionFixture.cs` - Test fixture following NUnit conventions

### Build & Test Setup
- FluentAssertions 6.12.1 provides excellent assertion syntax
- Project reference works cleanly between test and main project
- All 4 required tests pass: basic passing, exception throwing, expression text, file/line info

## Expression Tree Runtime (Increment 2)

### Key Discoveries
- Expression trees cannot contain local functions - must use instance or static methods
- ConcurrentDictionary provides thread-safe caching for expression evaluation results
- Single evaluation requirement achieved by evaluating operands once and caching results
- Custom binary operation evaluation prevents double-evaluation of the entire expression

### Technical Insights
- ExpressionAnalyzer uses visitor pattern with caching to ensure each sub-expression evaluates exactly once
- Binary comparison analysis extracts left/right operand values and formats them clearly
- Error message format: "Assertion failed: {expr} at {file}:{line}\n  Left: {value}\n  Right: {value}"
- BinaryOp enum supports all C# comparison operators: ==, !=, <, <=, >, >=

### Implementation Structure
- `/SharpAssert/SharpInternal.cs` - Internal API for expression tree analysis
- `/SharpAssert/ExpressionAnalyzer.cs` - Core expression tree visitor and analyzer
- `/SharpAssert/BinaryOp.cs` - Enumeration of binary comparison operators
- `/SharpAssert.Tests/ExpressionAnalysisFixture.cs` - Comprehensive test coverage

### Testing Insights
- All comparison operators tested: equality, inequality, less/greater than variants
- Null value handling works correctly, shows "null" in output
- Complex expression single-evaluation verified with side-effect tracking
- 8 total tests passing (4 foundation + 4 expression analysis)