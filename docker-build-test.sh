#!/bin/bash

# Docker Build Simulation Script
# This script simulates the Docker build process to help debug build issues

echo "=== Docker Build Simulation ==="

# Create temporary build directory
BUILD_DIR="./docker-build-test"
echo "Creating build directory: $BUILD_DIR"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR/src"

# Step 1: Copy project files (simulate Docker COPY commands)
echo "Step 1: Copying project files..."
cp -r src/ "$BUILD_DIR/"

# Step 2: Restore dependencies
echo "Step 2: Restoring dependencies..."
cd "$BUILD_DIR"
dotnet restore "src/SetlistStudio.Web/SetlistStudio.Web.csproj" \
    --source https://api.nuget.org/v3/index.json \
    --verbosity minimal

if [ $? -ne 0 ]; then
    echo "ERROR: Restore failed"
    exit 1
fi

# Step 3: Build the application
echo "Step 3: Building application..."
cd "src/SetlistStudio.Web"
dotnet build "SetlistStudio.Web.csproj" \
    -c Release \
    -o "../../app/build" \
    --no-restore \
    --verbosity minimal \
    -p:TreatWarningsAsErrors=true \
    -p:WarningsAsErrors="" \
    -p:WarningsNotAsErrors="NU1603"

BUILD_RESULT=$?

if [ $BUILD_RESULT -eq 0 ]; then
    echo "SUCCESS: Build completed successfully"
    echo "Build artifacts:"
    ls -la "../../app/build/" | head -10
else
    echo "ERROR: Build failed with exit code $BUILD_RESULT"
    echo "Source files in context:"
    find "../.." -name "*.cs" | head -20
    exit 1
fi

# Step 4: Publish
echo "Step 4: Publishing application..."
dotnet publish "SetlistStudio.Web.csproj" \
    -c Release \
    -o "../../app/publish" \
    --no-restore \
    --no-build \
    --verbosity minimal

PUBLISH_RESULT=$?

if [ $PUBLISH_RESULT -eq 0 ]; then
    echo "SUCCESS: Publish completed successfully"
    echo "Published artifacts:"
    ls -la "../../app/publish/" | head -10
else
    echo "ERROR: Publish failed with exit code $PUBLISH_RESULT"
    exit 1
fi

# Cleanup
cd "../../../"
echo "Cleaning up build directory..."
rm -rf "$BUILD_DIR"

echo "=== Docker Build Simulation Complete ==="