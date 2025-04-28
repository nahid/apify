#!/bin/bash
# Test script for interactive init command

# Set the dotnet path to ensure we use the right SDK
export DOTNET_PATH="/nix/store/8y83na7h81fw5kr1msmjc7rvx329djp0-dotnet-sdk-8.0.300/bin"
export PATH="$DOTNET_PATH:$PATH"

# Clean up from previous runs
echo "Cleaning up from previous runs..."
rm -f apify-config.json
rm -rf .apify

# Build the project first to ensure it's up to date
echo "Building the project..."
dotnet build

# Run the init command and pipe in responses
echo "Running interactive init command..."
# Create a temporary input file with exact answers
cat > temp_input.txt << EOL
y
y
Test API Project
Development
https://jsonplaceholder.typicode.com
n
n
EOL

# Run the init command with input from the file
cat temp_input.txt | dotnet run init

# Clean up
rm temp_input.txt

# Verify files were created
echo
echo "=== Verifying output files ==="
if [ -f "apify-config.json" ]; then
    echo "✅ Config file created successfully"
else
    echo "❌ Config file not created"
fi

if [ -d ".apify" ]; then
    echo "✅ API directory created successfully"
    echo "   Found files:"
    ls -la .apify/
else
    echo "❌ API directory not created"
fi

# Display the created config file
echo
echo "=== Displaying config file content ==="
if [ -f "apify-config.json" ]; then
    cat apify-config.json
else
    echo "Config file not found"
fi