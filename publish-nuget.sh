#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Publishing SharpAssert to NuGet.org${NC}"
echo "============================================"

# Check if NUGET_API_KEY is set
if [ -z "$NUGET_API_KEY" ]; then
    echo -e "${RED}❌ Error: NUGET_API_KEY environment variable is not set${NC}"
    echo -e "${YELLOW}Please set it with: export NUGET_API_KEY=your_api_key${NC}"
    exit 1
fi

# Get version from command line or use default
VERSION=${1:-1.0.0}

echo -e "${YELLOW}📦 Building and packing version: $VERSION${NC}"

# Clean previous packages
rm -rf ./packages
mkdir -p ./packages

# Restore dependencies
echo -e "${BLUE}📥 Restoring dependencies...${NC}"
dotnet restore

# Build in Release mode
echo -e "${BLUE}🔨 Building solution...${NC}"
dotnet build --configuration Release --no-restore

# Run tests to ensure everything works
echo -e "${BLUE}🧪 Running tests...${NC}"
dotnet test --configuration Release --no-build

# Pack both projects
echo -e "${BLUE}📦 Creating NuGet packages...${NC}"
dotnet pack SharpAssert/SharpAssert.csproj \
    --configuration Release \
    --no-build \
    --output ./packages \
    -p:PackageVersion="$VERSION"

dotnet pack SharpAssert.Rewriter/SharpAssert.Rewriter.csproj \
    --configuration Release \
    --no-build \
    --output ./packages \
    -p:PackageVersion="$VERSION"

# Test the package locally before publishing
echo -e "${BLUE}🔍 Testing package locally...${NC}"
cd SharpAssert.PackageTest
chmod +x test-local-package.sh
./test-local-package.sh
cd ..

# Show what we're about to publish
echo -e "${YELLOW}📋 Packages to publish:${NC}"
ls -la ./packages/*.nupkg

# Confirm before publishing
echo -e "${YELLOW}⚠️  About to publish to NuGet.org!${NC}"
read -p "Are you sure you want to publish version $VERSION? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${RED}❌ Publishing cancelled${NC}"
    exit 1
fi

# Publish to NuGet
echo -e "${BLUE}🚀 Publishing to NuGet.org...${NC}"
for package in ./packages/*.nupkg; do
    echo -e "${BLUE}📤 Publishing $(basename $package)...${NC}"
    dotnet nuget push "$package" \
        --api-key "$NUGET_API_KEY" \
        --source https://api.nuget.org/v3/index.json \
        --skip-duplicate
done

echo -e "${GREEN}✅ Successfully published SharpAssert $VERSION to NuGet.org!${NC}"
echo -e "${BLUE}🔗 View your package at: https://www.nuget.org/packages/SharpAssert/${NC}"
echo ""
echo -e "${YELLOW}💡 Next steps:${NC}"
echo -e "  1. Create a git tag: git tag v$VERSION && git push origin v$VERSION"
echo -e "  2. Create a GitHub release for v$VERSION"
echo -e "  3. Wait a few minutes for NuGet indexing to complete"