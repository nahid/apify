# Apify

A powerful C# CLI application for comprehensive API testing, enabling developers to streamline API validation workflows with rich configuration and execution capabilities.

## Features

- **Multiple Request Methods**: Support for GET, POST, PUT, DELETE, and more
- **Rich Payload Types**: JSON, Text, Form Data
- **File Upload Support**: Test multipart/form-data requests with file uploads
- **Environment Variables**: Use different configurations for development, staging, production
- **Test Assertions**: Validate response status, headers, body content
- **Detailed Reports**: Comprehensive output with request and response details
- **Single File Deployment**: Simplified deployment as a single executable file
- **.NET 9 Ready**: Automatic multi-targeting with .NET 9.0 when available

## Getting Started

### Prerequisites

- .NET 8.0 SDK (required)
- .NET 9.0 SDK (optional, for building with .NET 9.0 when available)

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/api-tester.git
cd api-tester

# Build the project
dotnet build

# Run the application
dotnet run
```

### Single File Build

For simplified deployment, you can build a single executable file:

```bash
# Build a single file executable
dotnet publish -c Release -r linux-x64 --self-contained true
# Windows:
# dotnet publish -c Release -r win-x64 --self-contained true
# macOS:
# dotnet publish -c Release -r osx-x64 --self-contained true
# macOS ARM64:
# dotnet publish -c Release -r osx-arm64 --self-contained true
```

## Usage

### Integration into Existing API Projects

You can use Apify within your existing API projects:

1. **Build and Install Apify**
   ```bash
   # Build as a single file executable
   dotnet publish -c Release -r <your-platform> --self-contained true
   
   # Copy the executable to a location in your PATH, or use it directly
   cp bin/Release/net8.0/<your-platform>/publish/apify /usr/local/bin/
   # or for Windows
   # copy bin\Release\net8.0\win-x64\publish\apify.exe C:\path\to\your\bin\
   ```

2. **Initialize API Testing in Your Project Directory**
   ```bash
   # Navigate to your API project
   cd /path/to/your/weatherapi
   
   # Initialize API testing (creates apify-config.json and .apify/ directory)
   apify init --name "Weather API Tests" --base-url "https://api.weather.com"
   # If you didn't install globally, use the full path to the executable
   # /path/to/apify init --name "Weather API Tests" --base-url "https://api.weather.com"
   ```

3. **Create API Test Definitions**
   - You can use the interactive command to create API request definitions:
     ```bash
     # Create a new API request in .apify/users/all.json
     apify create request --file users.all
     ```
   - Or manually add test files to the `.apify/` directory in your project
   - Each JSON file represents an API endpoint test
   - You can use dot notation for nested directories (e.g., `users.all` for `.apify/users/all.json`)

4. **Run Tests**
   ```bash
   # From your project directory where apify-config.json is located
   apify run users.all
   
   # Run with verbose output
   apify run users.all --verbose
   
   # Run with a specific environment
   apify run users.all --env Production
   ```

5. **View Environment Configuration**
   ```bash
   # From your project directory
   apify list-env
   ```

### Direct Usage (without installation)

### Initialize a Project

```bash
dotnet run init --name "My API Project" --base-url "https://api.example.com"
```

### Create API Requests

```bash
# Create a new API request interactively
dotnet run create request --file users.all
```

### Run API Tests

```bash
# Run a specific test
dotnet run run users.all

# Run with verbose output
dotnet run run users.all --verbose

# Run with a specific environment
dotnet run run users.all --env Production
```

### List Environments

```bash
dotnet run list-env
```

## CI/CD with GitHub Actions

This project includes GitHub Actions workflows for continuous integration and releases.

### Continuous Integration

The CI workflow runs on every push to the main branch and pull requests. It:

1. Builds the project on Windows, Linux, and macOS
2. Runs API tests to verify functionality
3. Ensures compatibility across all supported platforms

### Release Process

To create a release:

1. **Tag-based release**:
   - Create and push a new tag: `git tag -a v1.0.0 -m "Version 1.0.0" && git push origin v1.0.0`

2. **Manual release**:
   - Go to the Actions tab in your GitHub repository
   - Select the "Release" workflow
   - Click "Run workflow"
   - Enter the version number (e.g., "1.0.0")

This will:
- Build single file executables for Windows, Linux, macOS (x64), and macOS (ARM64)
- Create a GitHub release with the executables attached
- Tag the release with the specified version

## API Test Definition

API tests are defined in JSON files with this structure:

```json
{
  "Name": "Sample API Test",
  "Description": "Tests the sample endpoint",
  "Uri": "https://api.example.com/endpoint",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "Authorization": "Bearer {{token}}"
  },
  "Payload": {
    "name": "John Doe",
    "email": "john@example.com"
  },
  "PayloadType": "json",
  "Tests": [
    {
      "Name": "Status code is successful",
      "AssertType": "StatusCode",
      "ExpectedValue": "200"
    }
  ]
}
```

## Example: Testing a Weather API

Here's a practical example of how to use Apify with a Weather API project:

### Step 1: Setup

```bash
# Navigate to your Weather API project
cd ~/projects/weatherapi

# Initialize API testing (creates apify-config.json and .apify/ directory)
apify init --name "Weather API Tests" --base-url "https://api.weather.com"
```

### Step 2: Create API Test Definitions

Create a file `.apify/weather/current.json` using the interactive command:
```bash
apify create request --file weather.current
```

Or manually create a file with the following content:

```json
{
  "Name": "Get Current Weather",
  "Description": "Tests the current weather endpoint",
  "Uri": "{{baseUrl}}/current?location={{location}}",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "X-API-Key": "{{apiKey}}"
  },
  "Tests": [
    {
      "Name": "Status code is 200",
      "AssertType": "StatusCode",
      "ExpectedValue": "200"
    },
    {
      "Name": "Response contains temperature",
      "Assertion": "response.body.$.current.temperature exists"
    },
    {
      "Name": "Response contains location details",
      "Assertion": "response.body.$.location.name == {{location}}"
    }
  ]
}
```

### Step 3: Configure Environment Variables

Edit `apify-config.json` to add your environment variables:

```json
{
  "Name": "Default",
  "Description": "Weather API Configuration",
  "DefaultEnvironment": "Development",
  "Environments": [
    {
      "Name": "Development",
      "Description": "Development environment",
      "Variables": {
        "baseUrl": "https://api.weather.com/v1",
        "location": "New York",
        "apiKey": "your-dev-api-key"
      }
    },
    {
      "Name": "Production",
      "Description": "Production environment",
      "Variables": {
        "baseUrl": "https://api.weather.com/v1",
        "location": "New York",
        "apiKey": "your-prod-api-key"
      }
    }
  ]
}
```

### Step 4: Run the Tests

```bash
# From your Weather API project directory
apify run weather.current --verbose
```

## License

[MIT License](LICENSE)