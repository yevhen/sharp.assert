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