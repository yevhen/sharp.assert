#!/bin/bash
set -e

echo "🔧 Setting up SharpAssert Package Test..."

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Build and pack the main project
echo "📦 Building and packing SharpAssert..."
cd "$ROOT_DIR"
dotnet build SharpAssert/SharpAssert.csproj --configuration Release
dotnet pack SharpAssert/SharpAssert.csproj --configuration Release --output ./packages --no-build

# Get the generated package version
PACKAGE_FILE=$(ls ./packages/SharpAssert.*.nupkg | head -n1)
PACKAGE_NAME=$(basename "$PACKAGE_FILE" .nupkg)
echo "📋 Found package: $PACKAGE_NAME"

# Extract version from package name (format: SharpAssert.X.Y.Z-suffix)
VERSION=$(echo "$PACKAGE_NAME" | sed 's/SharpAssert\.//')
echo "📋 Package version: $VERSION"

# Update the package reference version in the project file
cd "$SCRIPT_DIR"
echo "🔧 Updating package reference to version $VERSION..."

# Update the SharpAssert package version in the existing project file
# This works on both macOS and Linux
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s/<PackageReference Include=\"SharpAssert\" Version=\"[^\"]*\"/<PackageReference Include=\"SharpAssert\" Version=\"$VERSION\"/" SharpAssert.PackageTest.csproj
else
    # Linux
    sed -i "s/<PackageReference Include=\"SharpAssert\" Version=\"[^\"]*\"/<PackageReference Include=\"SharpAssert\" Version=\"$VERSION\"/" SharpAssert.PackageTest.csproj
fi

echo "🧹 Cleaning package test project..."
dotnet clean

echo "🔧 Restoring packages from local source..."
dotnet restore --source "$ROOT_DIR/packages" --source https://api.nuget.org/v3/index.json

echo "🏗️ Building package test project..."
dotnet build

echo "🧪 Running package tests..."
dotnet test --verbosity normal

echo "✅ Package test completed successfully!"
echo "💡 The SharpAssert package is working correctly with interceptors."