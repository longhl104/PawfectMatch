#!/bin/bash

# PostGIS Installer Lambda Build and Deploy Script

set -e

# Configuration
LAMBDA_DIR="/Volumes/T7Shield/Projects/PawfectMatch/Environment/Lambdas/InstallPostgis"
PROJECT_DIR="$LAMBDA_DIR/src/InstallPostgis"
BUILD_CONFIG="Release"
TARGET_FRAMEWORK="net9.0"

echo "Building PostGIS Installer Lambda function..."

# Navigate to the project directory
cd "$PROJECT_DIR"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean -c $BUILD_CONFIG

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

# Build the project
echo "Building the project..."
dotnet build -c $BUILD_CONFIG --no-restore

# Run tests
echo "Running tests..."
cd "$LAMBDA_DIR"
dotnet test --no-build -c $BUILD_CONFIG

# Publish for Lambda deployment
echo "Publishing Lambda deployment package..."
cd "$PROJECT_DIR"
dotnet publish -c $BUILD_CONFIG -o "bin/$BUILD_CONFIG/$TARGET_FRAMEWORK/publish" --no-build

echo "PostGIS Installer Lambda function build completed successfully!"
echo "Published artifacts are in: $PROJECT_DIR/bin/$BUILD_CONFIG/$TARGET_FRAMEWORK/publish"

# Verify the publish directory contains the expected files
PUBLISH_DIR="$PROJECT_DIR/bin/$BUILD_CONFIG/$TARGET_FRAMEWORK/publish"
if [ -f "$PUBLISH_DIR/InstallPostgis.dll" ]; then
    echo "✅ Main assembly found: InstallPostgis.dll"
else
    echo "❌ Main assembly not found: InstallPostgis.dll"
    exit 1
fi

if [ -f "$PUBLISH_DIR/InstallPostgis.deps.json" ]; then
    echo "✅ Dependencies file found: InstallPostgis.deps.json"
else
    echo "❌ Dependencies file not found: InstallPostgis.deps.json"
    exit 1
fi

echo ""
echo "Lambda function is ready for deployment!"
echo "The CDK deployment will use the published artifacts from:"
echo "$PUBLISH_DIR"
