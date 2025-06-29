#!/bin/bash

# Build and pack the NuGet package
echo "Building and packing Longhl104.PawfectMatch..."

# Remove previous package folder
if [ -d "./nupkg" ]; then
    echo "🗑️  Removing previous nupkg folder..."
    rm -rf ./nupkg
fi

# Clean previous builds
dotnet clean

# Build the project
dotnet build --configuration Release

# Pack the project
dotnet pack --configuration Release --no-build

# Check if pack was successful
if [ $? -eq 0 ]; then
    echo "✅ Package created successfully!"
    echo "📦 Package location: ./nupkg/"
    ls -la ./nupkg/
else
    echo "❌ Package creation failed!"
    exit 1
fi
