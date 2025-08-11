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
├── SharpAssert.Rewriter/           # MSBuild source rewriter
├── SharpAssert.Tests/              # Unit tests for main library
├── SharpAssert.Rewriter.Tests/     # Unit tests for rewriter
├── SharpAssert.PackageTest/        # Package tests (local NuGet feed)
├── local-feed/                     # Local NuGet package feed
├── nuget.config                    # NuGet feed configuration
├── publish-local.sh                # Publish to local feed
├── test-local.sh                   # End-to-end testing
└── Directory.Build.props           # Centralized versioning
```

## 🧪 Testing Strategy

### Two Levels of Testing

1. **Unit Tests** (`SharpAssert.Tests` & `SharpAssert.Rewriter.Tests`)
   - Fast, focused tests for individual components
   - Run with: `dotnet test`

2. **Package Tests** (`SharpAssert.PackageTest`)
   - Tests the actual NuGet packages via local feed
   - Validates complete end-user experience including MSBuild rewriting
   - Run with: `./test-local.sh`

### Testing Workflow

```bash
# During development (unit tests)
dotnet test

# Before committing (comprehensive validation)
./test-local.sh

# Manual package testing
./publish-local.sh
dotnet test SharpAssert.PackageTest/
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

### Package Validation

SharpAssert uses a **Local NuGet Feed** approach for development and testing:

```bash
# Publish to local feed and test in one command
./test-local.sh

# Or run steps manually:
./publish-local.sh          # Publish packages to local-feed/
dotnet test SharpAssert.PackageTest/  # Test with local packages
```

### Local Development Benefits

- ✅ **Simple workflow** - Single command testing
- ✅ **No cache management** - Timestamp-based versioning
- ✅ **No file editing** - Stable wildcard package references
- ✅ **Professional approach** - Standard NuGet development pattern

### Package Test Project

The `SharpAssert.PackageTest` project verifies the **actual NuGet package** works correctly:

```bash
# Run the automated package test
cd SharpAssert.PackageTest
./test-local-package.sh
```

The script automatically:
1. 🧹 Cleans packages directory to avoid version conflicts
2. 📦 Builds and packs SharpAssert with `-local` suffix
3. 🔧 Updates test project to use the exact package version
4. 🧪 Runs comprehensive package validation tests
5. ✅ Confirms Assert calls are properly transformed

**Example output:**
```
🔧 SharpAssert Local Package Test
================================================
🧹 Cleaning packages directory...
📦 Building SharpAssert with local suffix...
✅ Built package: SharpAssert.1.0.0-local
📋 Package version: 1.0.0-local
🧪 Running package tests...
  ✓ Should_support_basic_assertions_via_package
  ✓ Should_provide_detailed_error_messages  
✅ All package tests passed!
🎉 SharpAssert packages work correctly from local feed
```
This serves as the **automated Clean Install Test** to ensure packaging works correctly.

## Development workflow
- Use `./publish-local.sh` to update local packages
- Run `./test-local.sh` for comprehensive validation
- Local feed uses timestamp versioning to avoid cache issues

### Rewriter Development

When working on the MSBuild rewriter:

1. **Use Package Tests for validation**
   ```bash
   # Rewriter changes require package rebuild
   ./test-local.sh
   ```

2. **Inspect rewritten code**
   ```bash
   # Generated files are in:
   # SharpAssert.PackageTest/obj/Debug/net9.0/SharpRewritten/
   ```

3. **Debug the rewriter**
   - Set breakpoints in `SharpLambdaRewriteTask` or `SharpAssertRewriter`
   - Use verbose MSBuild logging: `dotnet build -v diagnostic`
   - Check #line directives in generated files

### Adding New Features

1. **Write failing test first** (TDD)
2. **Implement minimal code** to pass the test
3. **Refactor** if needed
4. **Update integration tests** if behavior changes
5. **Test the package** with `test-local-package.sh`
6. **Update documentation** if adding public API

## 🐛 Debugging Issues

### Common Problems

**Rewriter not working:**
- Check both `SharpAssert` and `SharpAssert.Rewriter` packages are installed
- Verify MSBuild output shows "SharpAssert: Rewriting X source files"
- Look for rewritten files in `obj/Debug/net9.0/SharpRewritten/`

**Package test fails:**
- Clean local feed: `rm -rf local-feed`
- Rebuild packages: `./publish-local.sh`
- Check wildcard versioning: `1.0.0-dev*` in PackageTest.csproj

**Local feed issues:**
- Verify `nuget.config` points to `./local-feed`
- Check timestamp versioning prevents cache conflicts
- Ensure package source mapping directs SharpAssert* packages to local feed

## 📝 Commit Guidelines

### Commit Message Format
Write clear, descriptive commit messages that explain what the change does:

```
Add support for collection assertions
Handle null expressions in rewriter
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

Fix rewriter generation for complex expressions
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