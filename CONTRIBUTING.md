# Contributing to SharpAssert

Thank you for your interest in contributing to SharpAssert! This guide will help you get started with development.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- C# 12.0 compatible IDE (VS 2022 17.7+, Rider 2023.3+, VS Code)

### Initial Setup
```bash
# Clone the repository
git clone https://github.com/yevhen/SharpAssert.git
cd SharpAssert

# Build the solution
dotnet build

# Run all tests
dotnet test
```

## 📁 Project Structure

```
SharpAssert/
├── SharpAssert/                    # Main library with Assert methods
├── SharpAssert.Generators/         # Source generator for interceptors
├── SharpAssert.Tests/              # Unit tests for main library
├── SharpAssert.Generators.Tests/   # Unit tests for generator
├── SharpAssert.IntegrationTest/    # Integration tests (direct references)
├── SharpAssert.PackageTest/        # Package tests (NuGet reference)
└── Directory.Build.props           # Centralized versioning
```

## 🧪 Testing Strategy

### Three Levels of Testing

1. **Unit Tests** (`SharpAssert.Tests` & `SharpAssert.Generators.Tests`)
   - Fast, focused tests for individual components
   - Run with: `dotnet test SharpAssert.Tests`

2. **Integration Tests** (`SharpAssert.IntegrationTest`)
   - Tests interceptor functionality with direct project references
   - Immediate feedback on generator changes
   - Run with: `dotnet test SharpAssert.IntegrationTest`

3. **Package Tests** (`SharpAssert.PackageTest`)
   - Tests the actual NuGet package
   - Validates end-user experience
   - Run with: `cd SharpAssert.PackageTest && ./test-local-package.sh`

### Testing Workflow

```bash
# During development (fast iteration)
dotnet test SharpAssert.IntegrationTest

# Before committing (comprehensive)
dotnet test

# Validate package (final check)
cd SharpAssert.PackageTest
./test-local-package.sh
```

## 📦 Package Versioning

### Version Management

Versions are centrally managed in `Directory.Build.props`:

```xml
<Version>1.0.0</Version>
<VersionSuffix Condition="'$(CI)' != 'true'">local</VersionSuffix>
```

- **Local builds**: Automatically get `-local` suffix (e.g., `1.0.0-local`)
- **CI builds**: No suffix by default (e.g., `1.0.0`)
- **Custom version**: `dotnet build -p:Version=2.0.0`

### Creating a Local Package

```bash
# Build with local suffix
dotnet pack SharpAssert/SharpAssert.csproj -p:VersionSuffix=local

# Build with specific version
dotnet pack SharpAssert/SharpAssert.csproj -p:Version=1.2.3-beta

# Test the package
cd SharpAssert.PackageTest
./test-local-package.sh
```

## 🔧 Development Tips

### Generator Development

When working on the source generator:

1. **Use Integration Tests for rapid iteration**
   ```bash
   # Generator changes are picked up immediately
   dotnet build SharpAssert.Generators
   dotnet test SharpAssert.IntegrationTest
   ```

2. **Inspect generated code**
   ```bash
   # Generated files are in:
   # obj/Debug/net9.0/SharpAssert.Generators/SharpAssert.Generators.AssertInterceptorGenerator/
   ```

3. **Debug the generator**
   - Set breakpoints in generator code
   - Attach debugger to build process
   - Use `Debugger.Launch()` in generator code (remove before commit!)

### Adding New Features

1. **Write failing test first** (TDD)
2. **Implement minimal code** to pass the test
3. **Refactor** if needed
4. **Update integration tests** if behavior changes
5. **Test the package** with `test-local-package.sh`
6. **Update documentation** if adding public API

## 🐛 Debugging Issues

### Common Problems

**Interceptors not working:**
- Check `Features=InterceptorsPreview` in .csproj
- Verify `InterceptorsPreviewNamespaces` includes `SharpAssert.Generated`
- Look for generated files in obj/ directory

**Package test fails:**
- Clean packages directory: `rm -rf packages`
- Verify version in Directory.Build.props
- Check package reference in PackageTest.csproj

**Generator not running:**
- Clean and rebuild: `dotnet clean && dotnet build`
- Check generator is referenced as `OutputItemType="Analyzer"`
- Verify generator targets `netstandard2.0`

## 📝 Commit Guidelines

### Commit Message Format
Write clear, descriptive commit messages that explain what the change does:

```
Add support for collection assertions
Handle null expressions in interceptor
Add edge cases for async assertions
Update contributing guidelines
```

### Guidelines:
- Use imperative mood ("Add feature" not "Added feature")
- Start with a capital letter
- No period at the end of the subject line
- Keep the subject line under 50 characters when possible
- Use the body to explain **why** not **what** if needed

Examples:
```
Add comprehensive unit tests for SharpLambdaRewriteTask

Fix interceptor generation for complex expressions
- Handle nested method calls correctly
- Add support for null-conditional operators

Update README with new package test instructions
```

## 🚢 Release Process

### For Maintainers

1. **Update version** in `Directory.Build.props`
2. **Update CHANGELOG.md** with release notes
3. **Create git tag**: `git tag v1.2.3`
4. **Push tag**: `git push origin v1.2.3`
5. **CI/CD** will automatically:
   - Build release package
   - Run all tests
   - Publish to NuGet.org

### Version Numbering

Follow [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking API changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes


## 💡 Best Practices

1. **Always run tests** before committing
2. **Keep PRs focused** - one feature/fix per PR
3. **Write descriptive test names** that explain the scenario
4. **Update documentation** when changing public API
5. **Use the TodoWrite tool** for complex tasks
6. **Follow existing code style** - look at neighboring code

## 🤝 Getting Help

- **Questions**: Open a GitHub issue with `[Question]` prefix
- **Bugs**: Open issue with reproduction steps
- **Features**: Discuss in issue before implementing
- **Chat**: Join our [Discord/Slack/etc]

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.