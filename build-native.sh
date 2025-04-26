#!/bin/bash

# Build the native AOT version of API Tester
echo "Building Native AOT version of API Tester..."
echo "This might take a few minutes..."

# Clean up previous builds
echo "Cleaning up previous builds..."
rm -rf bin/Release

# Set environment variables for Native AOT
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export COMPlus_ReadyToRun=0
export COMPlus_TC_QuickJit=0
export COMPlus_InvokeHaltHard=0

# Publish with Native AOT
echo "Publishing with Native AOT..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true

if [ $? -eq 0 ]; then
    echo "Native AOT build completed successfully!"
    echo "Executable size:"
    ls -lh bin/Release/net8.0/linux-x64/publish/apitester
    echo ""
    echo "You can run the application with: ./bin/Release/net8.0/linux-x64/publish/apitester"
else
    echo "Native AOT build failed. Please check the error messages above."
fi