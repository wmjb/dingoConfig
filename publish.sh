#!/bin/bash

# Extract version from api.csproj
VERSION=$(grep -oP '(?<=<Version>)[^<]+' api/api.csproj)

if [ -z "$VERSION" ]; then
    echo "Error: Could not extract version from api/api.csproj"
    exit 1
fi

echo "Publishing dingoConfig version $VERSION"
echo "========================================="

# Create output directory
OUTPUT_DIR="publish/dingoConfig-$VERSION"
mkdir -p "$OUTPUT_DIR"

# Common publish arguments
PROJECT="api/api.csproj"
CONFIG="Release"
PUBLISH_ARGS="--self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true"

# Publish for Windows (x64)
echo ""
echo "Building for Windows (x64)..."
dotnet publish "$PROJECT" -c "$CONFIG" -r win-x64 $PUBLISH_ARGS -o "$OUTPUT_DIR/win-x64"
if [ $? -eq 0 ]; then
    echo "✓ Windows build complete"
else
    echo "✗ Windows build failed"
fi

# Publish for Linux (x64)
echo ""
echo "Building for Linux (x64)..."
dotnet publish "$PROJECT" -c "$CONFIG" -r linux-x64 $PUBLISH_ARGS -o "$OUTPUT_DIR/linux-x64"
if [ $? -eq 0 ]; then
    echo "✓ Linux build complete"
else
    echo "✗ Linux build failed"
fi

# Publish for macOS (x64 - Intel)
echo ""
echo "Building for macOS (x64 - Intel)..."
dotnet publish "$PROJECT" -c "$CONFIG" -r osx-x64 $PUBLISH_ARGS -o "$OUTPUT_DIR/osx-x64"
if [ $? -eq 0 ]; then
    echo "✓ macOS Intel build complete"
else
    echo "✗ macOS Intel build failed"
fi

# Publish for macOS (arm64 - Apple Silicon)
echo ""
echo "Building for macOS (arm64 - Apple Silicon)..."
dotnet publish "$PROJECT" -c "$CONFIG" -r osx-arm64 $PUBLISH_ARGS -o "$OUTPUT_DIR/osx-arm64"
if [ $? -eq 0 ]; then
    echo "✓ macOS Apple Silicon build complete"
else
    echo "✗ macOS Apple Silicon build failed"
fi

echo ""
echo "========================================="
echo "All builds complete!"
echo "Output directory: $OUTPUT_DIR"
echo ""
echo "Executables:"
echo "  Windows:             $OUTPUT_DIR/win-x64/dingoConfig.exe"
echo "  Linux:               $OUTPUT_DIR/linux-x64/dingoConfig"
echo "  macOS (Intel):       $OUTPUT_DIR/osx-x64/dingoConfig"
echo "  macOS (Apple Silicon): $OUTPUT_DIR/osx-arm64/dingoConfig"
