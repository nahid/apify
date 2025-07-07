# Apify Documentation

## Overview

Apify is a robust command-line tool designed for API testing and validation. It allows developers to define API tests in JSON format and execute them against endpoints, providing detailed output of the request, response, and test results. The tool offers centralized environment management, variable substitution, and support for various payload types and file uploads. It also includes a powerful mock server for simulating API responses during development and testing.

## Table of Contents

1. [Installation](#installation)
2. [Getting Started](#getting-started)
3. [Command Reference](#command-reference)
   - [Global Options](#global-options)
   - [Init Command](#init-command)
   - [Call Command](#call-command)
   - [Tests Command](#tests-command)
   - [List Environment Command](#list-env-command)
   - [Create Command](#create-command)
   - [Mock Server Command](#mock-server-command)
   - [About Command](#about-command)
4. [API Test Definition](#api-test-definition)
5. [Payload Types](#payload-types)
6. [File Uploads](#file-uploads)
7. [Test Assertions](#test-assertions)
8. [Configuration Properties](#configuration-properties)
9. [Variable System](#variable-system)
10. [Custom Variables](#custom-variables)
11. [Mock Server](#mock-server)
    - [Basic Mock Server](#basic-mock-server)
    - [Advanced Mock Server](#advanced-mock-server)
    - [Dynamic Responses](#dynamic-responses)
    - [Conditional Responses](#conditional-responses)
12. [Examples](#examples)
13. [Troubleshooting](#troubleshooting)

## Installation

To use Apify, you need to have .NET 8.0 SDK installed on your system.

```bash
# Clone the repository
git clone https://github.com/yourusername/api-tester.git
cd api-tester

# Build the project
dotnet build

# Run the application
dotnet run
```

### Native AOT Compilation

Apify supports Native AOT (Ahead-of-Time) compilation, which produces a self-contained executable with no dependency on the .NET runtime. This results in:

- Faster startup time
- Smaller deployment size
- No dependency on the .NET runtime
- Improved performance

To build the Native AOT version:

```bash
# Using the build script
./build-native.sh

# Or manually
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
```

The resulting executable will be located at:
`bin/Release/net8.0/linux-x64/publish/apify`

You can run it directly without needing the .NET runtime:

```bash
./bin/Release/net8.0/linux-x64/publish/apify
```

#### Platform-Specific Builds

For other platforms, replace `linux-x64` with your target platform:

- Windows: `win-x64`
- macOS: `osx-x64`
- ARM64: `linux-arm64` or `osx-arm64`

## Getting Started

### Initializing a New Project

To create a new API testing project with default configuration:

```bash
dotnet run init --name "My API Project"
```

This will:
- Create a `.apify` directory to store your API test definitions
- Generate a configuration file `apify-config.json` with development and production environments
- Create sample API test files in the `.apify` directory

### Running Your First Test

After initialization, you can run the sample test:

```bash
dotnet run call get
```

For more detailed output, use the verbose flag:

```bash
dotnet run call get --verbose
```

For debugging information, use the debug flag:

```bash
dotnet run call get --debug
```

## Command Reference

### Available Commands

| Command | Description |
|---------|-------------|
| `init` | Initialize a new API testing project |
| `call` | Execute an API test from a JSON definition file |
| `tests` | Run all tests in the project with visual progress indicators |
| `list-env` | List available environments |
| `create` | Create a new API request or mock response definition interactively |
| `mock-server` | Start an API mock server using mock definition files |
| `about` | Display information about the application (for debugging) |

### Global Options

The following options are available for all commands:

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--debug` | Show detailed debug output and stack traces | No | false |

The debug option enables detailed logging across all commands, showing:
- Stack traces for any errors
- Additional implementation details of request/response handling
- Detailed information about assertion evaluations
- Verbose information about file loading and processing

### Command Options

#### `init` Command

Initializes a new API testing project. Can be run interactively or with options for non-interactive setup.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--name` | The name of the API testing project | Yes | - |
| `--mock` | Create sample mock API definitions | No | false |
| `--force` | Force overwrite of existing files | No | false |

Example:
```bash
# Interactive initialization
dotnet run init

# Non-interactive initialization
dotnet run init --name "Payment API Tests" --mock
```

#### `call` Command

Executes a single API test from a definition file.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `file` | API definition file to test (supports dot notation) | Yes | - |
| `--verbose` or `-v` | Display detailed output | No | false |
| `--env` or `-e` | Environment to use from the profile | No | Profile's default |
| `--vars` | Runtime variables for the configuration | No | - |

Examples:
```bash
# Run a single test using dot notation
dotnet run call users.get

# Run a test with a specific environment and runtime variables
dotnet run call users.create --env Production --vars "userId=123,role=admin"
```

The `call` command supports simplified paths using dot notation:
- `users.get` will run `.apify/users/get.json`
- `auth.login` will run `.apify/auth/login.json`

The `.json` extension is optional.

#### `tests` Command

The tests command scans the project directory and runs all API tests found.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--verbose` or `-v` | Display detailed output including response body | No | false |
| `--dir` | The directory containing API test files | No | .apify |
| `--env` or `-e` | Environment to use from the profile | No | Profile's default |
| `--vars` | Runtime variables for the configuration | No | - |
| `--tag` | Filter tests by tag | No | - |

Example:
```bash
# Run all tests in the project
dotnet run tests

# Run all tests in a specific directory with verbose output
dotnet run tests --dir "apis" --verbose

# Run only tests with a specific tag
dotnet run tests --tag payments
```

#### `list-env` Command

Lists all available environments and their variables.

```bash
dotnet run list-env
```

#### `create` Command

A parent command for creating new API definitions.

##### `create request`

Creates a new API request definition interactively.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--file` | The file path for the new API request definition | Yes | - |
| `--force` | Force overwrite if the file already exists | No | false |

The command will interactively prompt for all necessary information, including HTTP method, URI, headers, payload, and assertions.

Example:
```bash
# Create a new API request interactively
dotnet run create request --file users.create
```

##### `create mock`

Creates a new mock API response definition interactively.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--file` | The file path for the new mock API response definition | Yes | - |
| `--force` or `-f` | Overwrite existing file if it exists | No | false |

The command will interactively prompt for all necessary information, including endpoint, HTTP method, status code, headers, and response body.

Example:
```bash
# Create a new mock API response interactively
dotnet run create mock --file users.get
```

#### `mock-server` Command

Starts a local API mock server using mock definition files.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--port` or `-p` | Port number to run the mock server on | No | 8080 |
| `--verbose` or `-v` | Enable verbose logging | No | false |
| `--directory` or `-d` | Directory containing mock definitions | No | ".apify" |

Example:
```bash
# Start the mock server on a custom port with verbose logging
dotnet run mock-server --port 3000 --verbose
```

#### `about` Command

Displays application information. This command is primarily for debugging purposes.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--force` | Force overwrite of existing files | No | false |

Example:
```bash
dotnet run about
```

## API Test Definition

API tests are defined in JSON files with the following structure:

```json
{
  "Name": "User API Test",
  "Description": "Tests the user endpoints",
  "Uri": "{{baseUrl}}/users/1",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "Authorization": "Bearer {{token}}"
  },
  "Payload": null,
  "PayloadType": "none",
  "Files": null,
  "Tests": [
    {
      "Name": "Status code is valid",
      "Description": "Checks if status code is 200",
      "AssertType": "StatusCode",
      "ExpectedValue": "200"
    }
  ],
  "Timeout": 30000,
  "Variables": {
    "customVar": "my-custom-value"
  },
  "Tags": ["users", "smoke"]
}
```

### Fields Explained

| Field | Type | Description | Required |
|-------|------|-------------|----------|
| `Name` | String | Name of the API test | Yes |
| `Description` | String | Description of the test | No |
| `Uri` | String | The endpoint URI (supports variable substitution) | Yes |
| `Method` | String | HTTP method (GET, POST, PUT, DELETE, etc.) | Yes |
| `Headers` | Object | HTTP headers as key-value pairs | No |
| `Payload` | String | The request body (can be JSON, text, etc.) | No |
| `PayloadType` | String | Type of payload (none, json, text, formData) | No |
| `Files` | Array | Files to upload (for multipart/form-data) | No |
| `Tests` | Array | Test assertions to validate the response | No |
| `Timeout` | Number | Request timeout in milliseconds | No |
| `Variables` | Object | Custom variables defined in the test file | No |
| `Tags` | Array | Tags for filtering tests | No |

## Payload Types

Apify supports multiple payload types for flexibility in testing different types of API endpoints.

| Payload Type | Description | Content-Type Header |
|--------------|-------------|---------------------|
| `none` | No request body | None |
| `json` | JSON structured data | application/json |
| `text` | Plain text content | text/plain |
| `formData` | URL-encoded form data | application/x-www-form-urlencoded |

## File Uploads

Apify supports file uploads using multipart/form-data.

```json
"Files": [
  {
    "Name": "Profile Picture",
    "FieldName": "avatar",
    "FilePath": "./images/profile.jpg",
    "ContentType": "image/jpeg"
  }
]
```

## Test Assertions

Apify provides comprehensive assertion capabilities to validate API responses.

### Supported Assertion Types

| Assertion Type | Description |
|----------------|-------------|
| `StatusCode` | Validates the HTTP status code |
| `ContainsProperty` | Checks if response JSON contains a specific property |
| `HeaderContains` | Validates a response header's value |
| `ResponseTimeBelow` | Verifies response time is below a threshold |
| `Equal` | Checks if a JSON property equals a specific value |
| `IsArray` | Checks if a JSON property is an array |
| `ArrayNotEmpty` | Verifies a JSON array is not empty |

## Configuration Properties

Apify uses a configuration file (`apify-config.json`) to store environment variables and project settings.

```json
{
  "Name": "My API Project",
  "DefaultEnvironment": "Development",
  "Variables": {
    "projectVar": "project-value"
  },
  "Environments": [
    {
      "Name": "Development",
      "Variables": {
        "baseUrl": "https://dev-api.example.com"
      }
    }
  ]
}
```

## Variable System

Apify provides a powerful variable system that supports template substitution in API definitions. Variables can be defined at three levels, in order of precedence:

1.  **Request-level variables** (highest priority)
2.  **Environment variables** (medium priority)
3.  **Project-level variables** (lowest priority)

Variables are referenced using `{{variableName}}`.

## Custom Variables

You can define custom variables directly in API definition files within the `Variables` object.

## Mock Server

The mock server feature allows you to create simulated API responses for testing.

### Basic Mock Server

Create a file with the `.mock.json` extension in your `.apify` directory.

```json
{
  "Name": "Get User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/:id",
  "StatusCode": 200,
  "ContentType": "application/json",
  "Response": {
    "id": 1,
    "name": "John Doe"
  }
}
```

### Advanced Mock Server

Use conditional responses for more complex scenarios.

```json
{
  "Name": "User API",
  "Method": "GET",
  "Endpoint": "/api/users/:id",
  "Responses": [
    {
      "Condition": "q.id == "1"",
      "StatusCode": 200,
      "Response": { "id": 1, "name": "John Doe" }
    },
    {
      "Condition": "true",
      "StatusCode": 404,
      "Response": { "error": "User not found" }
    }
  ]
}
```

### Dynamic Responses

The mock server supports dynamic responses using variable substitution.

Available template variables: `{{body}}`, `{{headers}}`, `{{query}}`, `{{path}}`, `{{timestamp}}`, `{{datetime}}`, `{{randomString}}`, `{{randomInt}}`.

### Conditional Responses

Conditions are JavaScript expressions with access to request data: `q` (query), `h` (headers), `b` (body), `p` (path).

## Examples

See the `Examples` directory in the `.apify` folder after running `init`.

## Troubleshooting

Use the `--verbose` and `--debug` flags to get more information about requests and errors.
