#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧪 Testing SharpAssert with Local Feed${NC}"
echo "======================================"

echo -e "${YELLOW}📦 Publishing latest packages to local feed...${NC}"
./publish-local.sh

echo -e "${YELLOW}🔄 Restoring packages from local feed...${NC}"
dotnet restore SharpAssert.PackageTest/ --verbosity quiet

echo -e "${YELLOW}🏗️ Building test project...${NC}"
dotnet build SharpAssert.PackageTest/ --verbosity quiet

echo -e "${YELLOW}🧪 Running package tests...${NC}"
echo ""

if dotnet test SharpAssert.PackageTest/ --no-build --verbosity normal; then
    echo ""
    echo -e "${GREEN}✅ All tests passed!${NC}"
    echo -e "${GREEN}🎉 SharpAssert packages work correctly from local feed${NC}"
else
    echo ""
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi