#!/bin/bash
set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}📦 Publishing SharpAssert to Local Feed${NC}"
echo "============================================"

# Create local-feed directory if it doesn't exist
mkdir -p local-feed

# Generate unique version based on timestamp for development
VERSION="1.0.0-dev$(date +%Y%m%d%H%M%S)"

echo -e "${YELLOW}🏗️ Building packages with version: $VERSION${NC}"

# Build and pack projects in dependency order  
echo -e "${BLUE}📦 Packing SharpAssert.Runtime...${NC}"
dotnet pack src/SharpAssert.Runtime/SharpAssert.Runtime.csproj \
    --configuration Release \
    --output local-feed \
    -p:PackageVersion="$VERSION" \
    --verbosity quiet

echo -e "${BLUE}📦 Packing SharpAssert (with local feed as source)...${NC}"
dotnet pack src/SharpAssert/SharpAssert.csproj \
    --configuration Release \
    --output local-feed \
    -p:PackageVersion="$VERSION" \
    --verbosity quiet \
    --source local-feed \
    --source https://api.nuget.org/v3/index.json

echo -e "${GREEN}✅ Published packages to local feed:${NC}"
echo -e "  📋 SharpAssert.Runtime $VERSION"
echo -e "  📋 SharpAssert $VERSION"
echo -e "${BLUE}📁 Feed location: ./local-feed/${NC}"
echo ""
echo -e "${YELLOW}💡 Now run: dotnet restore && dotnet test${NC}"