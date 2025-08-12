# Package Testing Implementation Plan

Based on the analysis and your feedback, here's the concrete implementation plan.

## Phase 1: Add Integration Tests Project ✅ COMPLETED

### 1.1 Create SharpAssert.IntegrationTests Project

```bash
# Create the project
dotnet new nunit -n SharpAssert.IntegrationTests -o SharpAssert.IntegrationTests
dotnet sln add SharpAssert.IntegrationTests
```

### 1.2 Configure Project File

Update `SharpAssert.IntegrationTests/SharpAssert.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <RootNamespace>SharpAssert.IntegrationTests</RootNamespace>
    <!-- Suppress PowerAssert warning -->
    <NoWarn>$(NoWarn);NETSDK1206</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference SharpAssert library normally -->
    <ProjectReference Include="..\SharpAssert\SharpAssert.csproj" />
    
    <!-- Reference Rewriter as MSBuild task/analyzer (critical difference!) -->
    <ProjectReference Include="..\SharpAssert.Rewriter\SharpAssert.Rewriter.csproj"
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="8.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="NUnit" Version="4.2.2" />
    <PackageReference Include="NUnit.Analyzers" Version="4.4.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="NUnit.Framework" />
  </ItemGroup>

</Project>
```

### 1.3 Add Basic Integration Tests

Create `SharpAssert.IntegrationTests/BasicIntegrationFixture.cs`:

```csharp
using static Sharp;

namespace SharpAssert.IntegrationTests;

[TestFixture]
public class BasicIntegrationFixture
{
    [Test]
    public void Assert_SimpleEquality_ProducesDetailedError()
    {
        var x = 5;
        var y = 10;
        
        var exception = Assert.Throws<SharpAssertionException>(() => 
            Assert(x == y));
        
        exception.Should().NotBeNull();
        exception.Message.Should().Contain("x == y");
        exception.Message.Should().Contain("x: 5");
        exception.Message.Should().Contain("y: 10");
    }
    
    [Test]
    public void Assert_ComplexExpression_AnalyzesSubExpressions()
    {
        var items = new[] { 1, 2, 3 };
        var target = 4;
        
        var exception = Assert.Throws<SharpAssertionException>(() => 
            Assert(items.Contains(target)));
        
        exception.Message.Should().Contain("items.Contains(target)");
        exception.Message.Should().Contain("[1, 2, 3]");
        exception.Message.Should().Contain("target: 4");
    }
}
```

## Phase 2: Create Package Testing Solution ✅ COMPLETED

### 2.1 Create Separate Solution File

```bash
# Create new solution for package testing
dotnet new sln -n SharpAssert.PackageTesting

# Add only the package test projects
dotnet sln SharpAssert.PackageTesting.sln add SharpAssert.PackageTest
dotnet sln SharpAssert.PackageTesting.sln add SharpAssert.PowerAssertTest
```

### 2.2 Create Isolated NuGet Config

Create `nuget.package-tests.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!-- Only use local feed for testing, no fallback to nuget.org during tests -->
    <clear />
    <add key="local-test" value="./local-feed" />
    <!-- Add nuget.org back but with lower priority -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  
  <!-- Ensure SharpAssert packages only come from local-test -->
  <packageSourceMapping>
    <packageSource key="local-test">
      <package pattern="SharpAssert*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

### 2.3 Update Package Test Projects to Share Files

Update `SharpAssert.PackageTest/SharpAssert.PackageTest.csproj`:

```xml
<ItemGroup>
  <!-- Link integration test files to avoid duplication -->
  <Compile Include="..\SharpAssert.IntegrationTests\**\*.cs" 
           Link="Shared\%(RecursiveDir)%(Filename)%(Extension)"
           Exclude="..\SharpAssert.IntegrationTests\obj\**;..\SharpAssert.IntegrationTests\bin\**" />
           
  <!-- Keep any package-specific tests in this project directly -->
</ItemGroup>
```

## Phase 3: Update Scripts for Isolation

### 3.1 Update test-local.sh

```bash
#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧪 Testing SharpAssert with Local Feed (Isolated)${NC}"
echo "=============================================="

# Define package cache directory for isolation
PACKAGE_CACHE="./test-packages"

echo -e "${YELLOW}🧹 Cleaning package cache...${NC}"
rm -rf $PACKAGE_CACHE

echo -e "${YELLOW}📦 Publishing latest packages to local feed...${NC}"
./publish-local.sh

echo -e "${YELLOW}🔄 Restoring packages from local feed (isolated cache)...${NC}"
dotnet restore SharpAssert.PackageTesting.sln \
  --packages $PACKAGE_CACHE \
  --configfile nuget.package-tests.config \
  --verbosity quiet

echo -e "${YELLOW}🏗️ Building package test projects...${NC}"
dotnet build SharpAssert.PackageTesting.sln \
  --packages $PACKAGE_CACHE \
  --no-restore \
  --verbosity quiet

echo -e "${YELLOW}🧪 Running package tests...${NC}"
echo ""

echo -e "${BLUE}📦 Running basic package tests...${NC}"
if ! dotnet test SharpAssert.PackageTest/ \
  --no-build \
  --no-restore \
  --verbosity normal; then
    echo -e "${RED}❌ Basic package tests failed${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}⚡ Running PowerAssert (forced mode) tests...${NC}"
if ! dotnet test SharpAssert.PowerAssertTest/ \
  --no-build \
  --no-restore \
  --verbosity normal; then
    echo -e "${RED}❌ PowerAssert forced mode tests failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}✅ All package tests passed!${NC}"
echo -e "${GREEN}📦 Packages validated in isolation (cache: $PACKAGE_CACHE)${NC}"
echo -e "${GREEN}🎉 No global cache pollution${NC}"

# Cleanup
echo -e "${YELLOW}🧹 Cleaning up test package cache...${NC}"
rm -rf $PACKAGE_CACHE
```

### 3.2 Create dev-test.sh for Development Testing

```bash
#!/bin/bash
set -e

# Quick test script for development (uses main solution)
echo "🧪 Running Development Tests (Integration + Unit)"
echo "=============================================="

# Just test the main solution (excludes package tests)
dotnet test SharpAssert.sln --verbosity minimal

echo "✅ Development tests passed"
echo "💡 Run ./test-local.sh to test NuGet packages"
```

## Phase 4: CI Pipeline Updates

### 4.1 Update GitHub Actions Workflow

Update `.github/workflows/build.yml`:

```yaml
- name: Run Unit and Integration Tests
  run: dotnet test SharpAssert.sln --verbosity normal

- name: Create Local Package
  run: ./publish-local.sh

- name: Test NuGet Packages (Isolated)
  run: |
    # Use isolated solution and cache
    dotnet restore SharpAssert.PackageTesting.sln \
      --packages ./ci-packages \
      --configfile nuget.package-tests.config
    
    dotnet build SharpAssert.PackageTesting.sln \
      --packages ./ci-packages \
      --no-restore
    
    dotnet test SharpAssert.PackageTesting.sln \
      --no-build \
      --no-restore
```

## Verification Checklist

After implementation:

- [x] `dotnet test SharpAssert.sln` runs unit + integration tests (fast dev cycle)
- [ ] `./test-local.sh` tests packages in isolation (no cache pollution)
- [x] Integration tests verify rewriter as MSBuild task
- [ ] Package tests verify NuGet package structure
- [ ] No test code duplication between projects
- [ ] CI tests all layers appropriately
- [ ] Global NuGet cache remains clean

## Benefits

1. **Faster Development** - Main solution excludes package tests
2. **Better Test Coverage** - Rewriter tested as MSBuild task
3. **Cache Safety** - Package tests use isolated cache
4. **Maintainability** - Shared test files, no duplication
5. **Clear Separation** - Dev tests vs package validation