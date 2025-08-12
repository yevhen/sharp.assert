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
├── SharpAssert.IntegrationTests/   # ★ Integration tests (MSBuild task)
├── SharpAssert.PackageTest/        # Package tests (local NuGet feed)
├── SharpAssert.PowerAssertTest/    # PowerAssert forced mode tests
├── SharpAssert.sln                 # Main development solution
├── SharpAssert.PackageTesting.sln  # Isolated package testing solution
├── local-feed/                     # Local NuGet package feed
├── nuget.config                    # Standard NuGet configuration
├── nuget.package-tests.config      # Isolated package testing configuration
├── dev-test.sh                     # Fast development testing
├── publish-local.sh                # Publish to local feed
├── test-local.sh                   # Complete package validation
└── Directory.Build.props           # Centralized versioning
```

## 🧪 Testing Strategy

SharpAssert uses a comprehensive **four-layer testing approach** to ensure reliability and maintainability across all development phases:

### 1. **Unit Tests** - Component-Level Validation
- **SharpAssert.Tests** - Tests core assertion logic without MSBuild integration
- **SharpAssert.Rewriter.Tests** - Tests the source rewriter components in isolation
- **Purpose**: Fast feedback during development, tests individual methods and classes
- **Run with**: `dotnet test SharpAssert.Tests/` or `dotnet test SharpAssert.Rewriter.Tests/`

### 2. **Integration Tests** - MSBuild Task Validation (New!)
- **SharpAssert.IntegrationTests** - Tests the rewriter as an actual MSBuild task during development
- **Purpose**: Validates that MSBuild integration works without requiring NuGet packages
- **Key Innovation**: Uses project references with `ReferenceOutputAssembly="false"` and direct import of `.targets` files
- **MSBuild Task Testing**: Overrides `SharpAssertRewriterPath` to use local build output
- **Run with**: `dotnet test SharpAssert.IntegrationTests/`

### 3. **Package Tests** - End-to-End NuGet Validation
- **SharpAssert.PackageTest** - Tests basic functionality via NuGet packages
- **SharpAssert.PowerAssertTest** - Tests PowerAssert forced mode via NuGet packages  
- **Purpose**: Validates complete end-user experience including packaging and MSBuild integration
- **Isolation**: Uses separate solution (`SharpAssert.PackageTesting.sln`) and NuGet config
- **Run with**: `./test-local.sh`

### 4. **Development Scripts** - Automated Workflows
- **`./dev-test.sh`** - Fast development testing (Unit + Integration only)
- **`./test-local.sh`** - Complete package validation with cache isolation

### Testing Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ Development Solutions                                        │
├─────────────────────────────────────────────────────────────┤
│ SharpAssert.sln (Main Solution)                            │
│ ├── SharpAssert.Tests (unit tests)                         │
│ ├── SharpAssert.Rewriter.Tests (unit tests)               │
│ ├── SharpAssert.IntegrationTests (MSBuild task tests) ★    │
│ └── ... (main projects)                                    │
├─────────────────────────────────────────────────────────────┤
│ SharpAssert.PackageTesting.sln (Package Isolation)         │
│ ├── SharpAssert.PackageTest (basic package tests)          │
│ └── SharpAssert.PowerAssertTest (PowerAssert mode tests)   │
└─────────────────────────────────────────────────────────────┘
```

### Integration Tests: MSBuild Task Testing Innovation

The **SharpAssert.IntegrationTests** project enables testing the MSBuild rewriter during development without packaging:

```xml
<!-- Project reference for build dependency only -->
<ProjectReference Include="..\SharpAssert.Rewriter\SharpAssert.Rewriter.csproj"
                  ReferenceOutputAssembly="false" />

<!-- Direct import of targets file (simulates NuGet package behavior) -->
<Import Project="..\SharpAssert.Rewriter\build\SharpAssert.Rewriter.targets" />

<!-- Override rewriter path to use local build output -->
<SharpAssertRewriterPath>$(MSBuildThisFileDirectory)..\SharpAssert.Rewriter\bin\$(Configuration)\net9.0\SharpAssert.Rewriter.dll</SharpAssertRewriterPath>
```

This approach:
- ✅ Tests MSBuild task functionality during development
- ✅ Eliminates need to package for every rewriter change  
- ✅ Provides fast feedback loop for MSBuild integration issues
- ✅ Simulates real NuGet package behavior without packaging overhead

### Package Testing Isolation

Package tests use complete isolation to prevent global NuGet cache pollution:

- **Separate Solution**: `SharpAssert.PackageTesting.sln` includes only package test projects
- **Isolated NuGet Config**: `nuget.package-tests.config` with package source mapping
- **Isolated Package Cache**: `./test-packages` directory (cleaned after each run)
- **Local Feed Only**: Forces SharpAssert packages to come from `./local-feed`

### Testing Workflow by Development Phase

```bash
# ⚡ Fast development (Unit + Integration) - No packaging required
./dev-test.sh

# 📦 Full validation before commits (includes package tests)
./test-local.sh

# 🔧 Manual testing workflows
dotnet test                                    # All tests in main solution
dotnet test SharpAssert.IntegrationTests/     # Just MSBuild integration tests
./publish-local.sh                            # Build packages to local feed
dotnet test SharpAssert.PackageTesting.sln    # Just package tests
```

### When to Use Each Test Layer

- **Unit Tests**: Fast development, testing individual methods/classes
- **Integration Tests**: Validating MSBuild task behavior, rewriter integration
- **Package Tests**: Before commits, validating end-user experience
- **Development Scripts**: Daily workflow automation

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

### Package Test Projects

The package testing solution includes **two specialized test projects**:

#### SharpAssert.PackageTest
- Tests **basic functionality** via NuGet packages  
- Validates core assertions work through complete packaging pipeline
- Uses normal SharpAssert configuration (with PowerAssert fallback)

#### SharpAssert.PowerAssertTest  
- Tests **PowerAssert forced mode** via NuGet packages
- Uses `UsePowerAssert=true` configuration to validate PowerAssert integration
- Ensures PowerAssert works correctly when explicitly enabled

Both test projects:
- ✅ Use **isolated package cache** to prevent global NuGet pollution
- ✅ Validate **actual NuGet packages** (not project references)  
- ✅ Test **complete MSBuild integration** including rewriter task
- ✅ Confirm **Assert calls are properly transformed**
- ✅ Run through `./test-local.sh` with full isolation

**Automated Testing Pipeline:**
```bash
# Complete package validation workflow
./test-local.sh
```

The script automatically:
1. 🧹 Creates isolated package cache (`./test-packages`)  
2. 📦 Publishes latest packages to local feed with timestamp versioning
3. 🔄 Restores packages using isolated NuGet configuration
4. 🏗️ Builds package test projects with isolated dependencies
5. 🧪 Runs both basic and PowerAssert package tests
6. 🎉 Validates complete end-user experience
7. 🧹 Cleans up isolated cache to prevent pollution

This serves as the **automated Clean Install Test** ensuring packaging works correctly across all usage modes.

## Development workflow
- Use `./publish-local.sh` to update local packages
- Run `./test-local.sh` for comprehensive validation
- Local feed uses timestamp versioning to avoid cache issues

### Rewriter Development

Working on the MSBuild rewriter now has **two validation approaches**:

#### Fast Development Cycle (Recommended)

1. **Use Integration Tests for rapid iteration**
   ```bash
   # Fast feedback - no packaging required
   dotnet test SharpAssert.IntegrationTests/
   # or use the dev script
   ./dev-test.sh
   ```

2. **Debug integration tests**
   - Set breakpoints in `SharpLambdaRewriteTask` or `SharpAssertRewriter`
   - Integration tests use your local build output directly
   - Inspect rewritten files in: `SharpAssert.IntegrationTests/obj/Debug/net9.0/SharpRewritten/`

#### Complete Package Validation

1. **Use Package Tests for final validation**
   ```bash
   # Complete end-to-end validation (slower)
   ./test-local.sh
   ```

2. **Debug package tests**
   - Inspect rewritten code in: `SharpAssert.PackageTest/obj/Debug/net9.0/SharpRewritten/`
   - Use verbose MSBuild logging: `dotnet build -v diagnostic`
   - Check #line directives in generated files

#### Integration Test Benefits for Rewriter Development

- ⚡ **No packaging step** - directly uses your build output
- 🔄 **Fast feedback loop** - change code, run test immediately  
- 🎯 **MSBuild task testing** - validates actual MSBuild integration
- 🔧 **Easy debugging** - breakpoints work seamlessly

### Adding New Features

1. **Write failing test first** (TDD)
2. **Implement minimal code** to pass the test
3. **Refactor** if needed
4. **Update integration tests** if behavior changes
5. **Test the package** with `test-local-package.sh`
6. **Update documentation** if adding public API

## 🐛 Debugging Issues

### Common Problems by Test Layer

**Integration Tests (SharpAssert.IntegrationTests) - MSBuild Task Issues:**
- Verify the rewriter project builds successfully: `dotnet build SharpAssert.Rewriter/`
- Check that `SharpAssertRewriterPath` points to correct assembly location
- Ensure `.targets` file is properly imported
- Look for rewritten files in `SharpAssert.IntegrationTests/obj/Debug/net9.0/SharpRewritten/`
- Use `dotnet build SharpAssert.IntegrationTests/ -v diagnostic` for detailed MSBuild logging

**Package Tests - End-to-End Issues:**
- **Rewriter not working**: Check both `SharpAssert` and `SharpAssert.Rewriter` packages are installed
- **Package test fails**: Clean local feed `rm -rf local-feed` then `./publish-local.sh`
- **Version conflicts**: Check wildcard versioning `1.0.0-dev*` in test projects
- **Isolation issues**: Ensure using `nuget.package-tests.config` and isolated cache

**Local Feed & Package Issues:**
- **Feed not found**: Verify `nuget.package-tests.config` points to `./local-feed`
- **Cache pollution**: Run `./test-local.sh` which uses isolated `./test-packages` cache
- **Version mismatches**: Check timestamp versioning prevents cache conflicts
- **Source mapping**: Ensure `packageSourceMapping` directs SharpAssert* packages to local feed

**MSBuild Rewriter Debugging:**
- **Set breakpoints** in `SharpLambdaRewriteTask` or `SharpAssertRewriter`
- **Verbose logging**: `dotnet build -v diagnostic` to see MSBuild task execution
- **Check generated files**: Look in `*/obj/Debug/net9.0/SharpRewritten/` directories
- **Verify #line directives** in generated files for proper source mapping

### Debugging Workflow

1. **Start with Integration Tests** - fastest feedback for MSBuild issues
2. **Use Package Tests** for complete validation - slower but comprehensive
3. **Check both solutions** - main solution vs package testing solution
4. **Clean and rebuild** when in doubt - `dotnet clean && dotnet build`

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

### Testing Best Practices

1. **Use the right test layer for the job**:
   - **Unit tests** for business logic and algorithms
   - **Integration tests** for MSBuild task behavior and rewriter validation
   - **Package tests** for complete end-to-end scenarios

2. **Follow the development testing workflow**:
   - Start with `./dev-test.sh` for rapid iteration
   - Use `./test-local.sh` before committing for full validation
   - Run integration tests when changing rewriter logic

3. **Always run tests** before committing - use `./test-local.sh` for complete validation

### Development Best Practices

1. **Keep PRs focused** - one feature/fix per PR
2. **Write descriptive test names** that explain the scenario  
3. **Update documentation** when changing public API
4. **Use the TodoWrite tool** for complex tasks
5. **Follow existing code style** - look at neighboring code

### MSBuild Rewriter Best Practices

1. **Start with integration tests** when developing rewriter features
2. **Test MSBuild integration early** - don't wait for packaging to catch integration issues
3. **Use isolated package testing** to prevent global cache pollution
4. **Verify generated code quality** - check the rewritten files in `obj/` directories

## 🤝 Getting Help

- **Questions**: Open a GitHub issue with `[Question]` prefix
- **Bugs**: Open issue with reproduction steps
- **Features**: Discuss in issue before implementing
- **Chat**: Join our [Discord/Slack/etc]

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.