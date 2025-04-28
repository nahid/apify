#!/bin/bash
# Test script for interactive init command

# Remove existing config if exists
rm -f apify-config.json

# Run the init command and pipe in responses
(
echo "y"  # Overwrite confirmation if needed
echo "Test API Project"  # Project name
echo ""  # Default environment name (just hit enter)
echo "https://jsonplaceholder.typicode.com"  # Base URL
echo "n"  # Don't configure additional variables
echo "n"  # Don't add additional environments
) | PATH="/nix/store/8y83na7h81fw5kr1msmjc7rvx329djp0-dotnet-sdk-8.0.300/bin:$PATH" dotnet run init

# Display the created config file
echo "=== Displaying generated config file ==="
cat apify-config.json