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
./scripts/publish-local.sh

echo -e "${YELLOW}🔄 Restoring packages from local feed (isolated cache)...${NC}"
dotnet restore SharpAssert.PackageTesting.sln \
  --packages $PACKAGE_CACHE \
  --configfile nuget.package-tests.config \
  -p:SharpAssertPackageVersion=1.0.0-local \
  --verbosity quiet

echo -e "${YELLOW}🏗️ Building package test projects...${NC}"
dotnet build SharpAssert.PackageTesting.sln \
  --packages $PACKAGE_CACHE \
  --no-restore \
  -p:SharpAssertPackageVersion=1.0.0-local \
  --verbosity quiet

echo -e "${YELLOW}🧪 Running package tests...${NC}"
echo ""

echo -e "${BLUE}📦 Running basic package tests...${NC}"
if ! dotnet test src/SharpAssert.PackageTest/ \
  --no-build \
  --no-restore \
  --verbosity normal; then
    echo -e "${RED}❌ Basic package tests failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}✅ All package tests passed!${NC}"
echo -e "${GREEN}📦 Packages validated in isolation (cache: $PACKAGE_CACHE)${NC}"
echo -e "${GREEN}🎉 No global cache pollution${NC}"

# Cleanup
echo -e "${YELLOW}🧹 Cleaning up test package cache...${NC}"
rm -rf $PACKAGE_CACHE