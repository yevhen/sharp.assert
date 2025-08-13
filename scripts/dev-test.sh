#!/bin/bash
set -e

# Quick test script for development (uses main solution)
echo "🧪 Running Development Tests (Integration + Unit)"
echo "=============================================="

# Just test the main solution (excludes package tests)
dotnet test SharpAssert.sln --verbosity minimal

echo "✅ Development tests passed"
echo "💡 Run ./test-local.sh to test NuGet packages"