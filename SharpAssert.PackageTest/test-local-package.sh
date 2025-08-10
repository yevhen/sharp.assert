#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔧 SharpAssert Local Package Test${NC}"
echo "================================================"

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Function to extract version from Directory.Build.props or project file
get_version() {
    local version=""
    
    # First try Directory.Build.props
    if [ -f "$ROOT_DIR/Directory.Build.props" ]; then
        version=$(grep '<Version>' "$ROOT_DIR/Directory.Build.props" | sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' | head -1)
    fi
    
    # Fallback to project file if not found
    if [ -z "$version" ] && [ -f "$ROOT_DIR/SharpAssert/SharpAssert.csproj" ]; then
        version=$(grep '<PackageVersion>' "$ROOT_DIR/SharpAssert/SharpAssert.csproj" | sed -n 's/.*<PackageVersion>\(.*\)<\/PackageVersion>.*/\1/p' | head -1)
    fi
    
    # Default if still not found
    if [ -z "$version" ]; then
        version="1.0.0"
    fi
    
    echo "$version"
}

# Clean packages directory to avoid picking up old packages
echo -e "${YELLOW}🧹 Cleaning packages directory...${NC}"
rm -rf "$ROOT_DIR/packages"
mkdir -p "$ROOT_DIR/packages"

# Build and pack with local suffix
echo -e "${YELLOW}📦 Building SharpAssert with local suffix...${NC}"
cd "$ROOT_DIR"

# Build with local version suffix for development
dotnet build SharpAssert/SharpAssert.csproj --configuration Release -p:VersionSuffix=local
dotnet pack SharpAssert/SharpAssert.csproj --configuration Release --output ./packages --no-build -p:VersionSuffix=local

# Get the actual package version (including suffix)
PACKAGE_FILE=$(find ./packages -name "SharpAssert.*.nupkg" -type f | head -n1)
if [ -z "$PACKAGE_FILE" ]; then
    echo -e "${RED}❌ No package file found in ./packages${NC}"
    exit 1
fi

PACKAGE_NAME=$(basename "$PACKAGE_FILE" .nupkg)
FULL_VERSION=$(echo "$PACKAGE_NAME" | sed 's/SharpAssert\.//')

echo -e "${GREEN}✅ Built package: $PACKAGE_NAME${NC}"
echo -e "${BLUE}📋 Package version: $FULL_VERSION${NC}"

# Update the PackageTest project
cd "$SCRIPT_DIR"
echo -e "${YELLOW}🔧 Updating PackageTest to use version $FULL_VERSION...${NC}"

# Update the SharpAssert package version in the project file
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s/<PackageReference Include=\"SharpAssert\" Version=\"[^\"]*\"/<PackageReference Include=\"SharpAssert\" Version=\"$FULL_VERSION\"/" SharpAssert.PackageTest.csproj
else
    # Linux
    sed -i "s/<PackageReference Include=\"SharpAssert\" Version=\"[^\"]*\"/<PackageReference Include=\"SharpAssert\" Version=\"$FULL_VERSION\"/" SharpAssert.PackageTest.csproj
fi

# Clean, restore, build and test
echo -e "${YELLOW}🧹 Cleaning test project...${NC}"
dotnet clean --verbosity quiet

echo -e "${YELLOW}📦 Restoring packages from local source...${NC}"
dotnet restore --source "$ROOT_DIR/packages" --source https://api.nuget.org/v3/index.json

echo -e "${YELLOW}🏗️ Building test project...${NC}"
dotnet build

echo -e "${YELLOW}🧪 Running package tests...${NC}"
if dotnet test --no-build --verbosity normal; then
    echo -e "${GREEN}✅ All package tests passed!${NC}"
    echo -e "${GREEN}🎉 The SharpAssert package (v$FULL_VERSION) works correctly with interceptors.${NC}"
else
    echo -e "${RED}❌ Some tests failed. Please check the output above.${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}ℹ️  Package Information:${NC}"
echo "  - Version: $FULL_VERSION"
echo "  - Location: $PACKAGE_FILE"
echo "  - Local feed: $ROOT_DIR/packages"