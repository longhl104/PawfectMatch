#!/bin/bash

# Script to publish the NuGet package to nuget.org

# Load environment variables from .env file if it exists
if [ -f .env ]; then
    source .env
fi

PACKAGE_SOURCE="https://api.nuget.org/v3/index.json"

echo "🚀 Publishing Longhl104.PawfectMatch to NuGet.org..."

# Build the package first
./build-package.sh

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Cannot publish."
    exit 1
fi

# Find the latest .nupkg file
PACKAGE_FILE=$(find ./nupkg -name "*.nupkg" -not -name "*.symbols.nupkg" | head -n 1)

if [ -z "$PACKAGE_FILE" ]; then
    echo "❌ No package file found!"
    exit 1
fi

echo "📦 Publishing package: $PACKAGE_FILE"

# Publish the package
dotnet nuget push "$PACKAGE_FILE" \
    --api-key "$API_KEY" \
    --source "$PACKAGE_SOURCE"

if [ $? -eq 0 ]; then
    echo "✅ Package published successfully!"
    echo "🌐 Package should be available at: https://www.nuget.org/packages/Longhl104.PawfectMatch/"
else
    echo "❌ Package publication failed!"
    exit 1
fi
