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
dotnet restore SharpAssert.PowerAssertTest/ --verbosity quiet
dotnet restore SharpAssert.SmartFallbackTest/ --verbosity quiet
dotnet restore SharpAssert.NoFallbackTest/ --verbosity quiet

echo -e "${YELLOW}🏗️ Building test projects...${NC}"
dotnet build SharpAssert.PackageTest/ --verbosity quiet
dotnet build SharpAssert.PowerAssertTest/ --verbosity quiet
dotnet build SharpAssert.SmartFallbackTest/ --verbosity quiet
dotnet build SharpAssert.NoFallbackTest/ --verbosity quiet

echo -e "${YELLOW}🧪 Running package tests...${NC}"
echo ""

echo -e "${BLUE}📦 Running basic package tests...${NC}"
if ! dotnet test SharpAssert.PackageTest/ --no-build --verbosity normal; then
    echo -e "${RED}❌ Basic package tests failed${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}⚡ Running PowerAssert (forced mode) tests...${NC}"
if ! dotnet test SharpAssert.PowerAssertTest/ --no-build --verbosity normal; then
    echo -e "${RED}❌ PowerAssert forced mode tests failed${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}🧠 Running Smart Fallback tests...${NC}"
if ! dotnet test SharpAssert.SmartFallbackTest/ --no-build --verbosity normal; then
    echo -e "${RED}❌ Smart fallback tests failed${NC}"
    exit 1
fi

echo ""
echo -e "${BLUE}🚫 Running No Fallback tests...${NC}"
if ! dotnet test SharpAssert.NoFallbackTest/ --no-build --verbosity normal; then
    echo -e "${RED}❌ No fallback tests failed${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}✅ All tests passed!${NC}"
echo -e "${GREEN}🎉 SharpAssert packages work correctly from local feed${NC}"
echo -e "${GREEN}⚡ PowerAssert integration validated successfully${NC}"