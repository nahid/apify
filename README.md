# API Tester

A powerful C# CLI application for comprehensive API testing, enabling developers to streamline API validation workflows with rich configuration and execution capabilities.

## Features

- **Multiple Request Methods**: Support for GET, POST, PUT, DELETE, and more
- **Rich Payload Types**: JSON, Text, Form Data
- **File Upload Support**: Test multipart/form-data requests with file uploads
- **Environment Variables**: Use different configurations for development, staging, production
- **Test Assertions**: Validate response status, headers, body content
- **Detailed Reports**: Comprehensive output with request and response details
- **Native AOT Support**: Improved performance with ahead-of-time compilation

## Getting Started

### Prerequisites

- .NET 8.0 SDK

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

### Native AOT Build

For improved performance, you can build using Native AOT:

```bash
# Build with Native AOT
./build-native.sh
# Or manually:
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

## Usage

### Initialize a Project

```bash
dotnet run init --name "My API Project" --base-url "https://api.example.com"
```

### Run API Tests

```bash
# Run a specific test
dotnet run run apis/sample-api.json

# Run with verbose output
dotnet run run apis/sample-api.json --verbose

# Run with a specific environment
dotnet run run apis/sample-api.json --env Production
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
- Build native AOT executables for Windows, Linux, macOS (x64), and macOS (ARM64)
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

## License

[MIT License](LICENSE)