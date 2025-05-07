# Apify Documentation

## Overview

Apify is a robust command-line tool designed for API testing and validation. It allows developers to define API tests in JSON format and execute them against endpoints, providing detailed output of the request, response, and test results. The tool offers centralized environment management, variable substitution, and support for various payload types and file uploads. It also includes a powerful mock server for simulating API responses during development and testing.

## Table of Contents

1. [Installation](#installation)
2. [Getting Started](#getting-started)
3. [Command Reference](#command-reference)
   - [Global Options](#global-options)
   - [Init Command](#init-command)
   - [Run Command](#run-command)
   - [Tests Command](#tests-command)
   - [List Environment Command](#list-env-command)
   - [Create Request Command](#create-request-command)
   - [Mock Server Command](#mock-server-command)
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
dotnet run init --name "My API Project" --base-url "https://api.example.com" 
```

This will:
- Create a `.apify` directory to store your API test definitions
- Generate a configuration file `apify-config.json` with development and production environments
- Create sample API test files in the `.apify` directory

### Running Your First Test

After initialization, you can run the sample test:

```bash
dotnet run run apis/sample-api.json
```

For more detailed output, use the verbose flag:

```bash
dotnet run run apis/sample-api.json --verbose
```

For debugging information, use the debug flag:

```bash
dotnet run run apis/sample-api.json --debug
```

## Command Reference

### Available Commands

| Command | Description |
|---------|-------------|
| `init` | Initialize a new API testing project |
| `run` | Execute API tests from JSON definition files |
| `tests` | Run all tests in the project with visual progress indicators |
| `list-env` | List available environments |
| `create request` | Create a new API request definition interactively |
| `mock-server` | Start an API mock server using mock definition files |

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

Initializes a new API testing project by creating necessary configuration files and directory structure. The command runs interactively, prompting for required information.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--force` | Force overwrite of existing files | No | false |
| `--debug` | Show detailed debug output and stack traces | No | false |

The command will interactively prompt for:
1. Project name (e.g., "Payment API Tests")
2. Base URL for API endpoints (e.g., "https://api.example.com")
3. Default environment (defaults to "Development")

Example:
```bash
# Run the initialization interactively
dotnet run init

# Use the executable directly
./apify init

# Force overwrite of existing configuration
dotnet run init --force
```

After running the command, you'll be guided through the setup process with prompts for each required piece of information.

#### `run` Command

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `files` | API definition files to test (supports wildcards) | Yes | - |
| `--profile` or `-p` | Configuration profile to use | No | "Default" |
| `--env` or `-e` | Environment to use from the profile | No | Profile's default |
| `--debug` | Show detailed debug output and stack traces | No | false |

Examples:
```bash
# Run a single test (full path)
dotnet run run apis/user-api.json

# Using dot notation (simplified): will run .apify/users/post.json
dotnet run run users.post

# Using dot notation with executable
./apify run users.post

# Run all tests in the apis directory
dotnet run run apis/*.json --debug

# Run tests using a specific environment
dotnet run run apis/payment-api.json --env Production

# Run with debug information
dotnet run run apis/payment-api.json --debug
```

The `run` command supports simplified paths using dot notation:
- `users.post` will run `.apify/users/post.json`
- `auth.login` will run `.apify/auth/login.json`
- `products.search` will run `.apify/products/search.json`

The `.json` extension is optional when using the `run` command. You can use the executable directly (`./apify run`) or with dotnet (`dotnet run run`).

#### `tests` Command

The tests command scans the project directory and runs all API tests found in the `.apify` directory and its subdirectories, providing a visual progress display during execution.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--env` or `-e` | Environment to use from the profile | No | Profile's default |
| `--tag` | Filter tests by tag | No | - |
| `--debug` | Show detailed debug output and stack traces | No | false |

Features of the tests command:
- Animated spinner showing active processing
- Clear progress counter (e.g., "Processing 1/5: sample-api.json") 
- Visual indication of currently executing test file with a highlighted box
- Real-time test results showing both placeholder circles and pass/fail indicators
- Comprehensive summary with statistics at the end

Example:
```bash
# Run all tests in the project
dotnet run tests

# Run all tests with detailed output
dotnet run tests --debug

# Run only tests with a specific tag
dotnet run tests --tag payments

# Run with debug information
dotnet run tests --debug
```

#### `list-env` Command

Lists all available environments and their variables.

```bash
dotnet run list-env

# With debug information
dotnet run list-env --debug
```

#### `create request` Command

Creates a new API request definition interactively.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--file` | The file path for the new API request definition | Yes | - |
| `--force` | Force overwrite if the file already exists | No | false |
| `--debug` | Show detailed debug output and stack traces | No | false |

The file path supports dot notation for creating nested directories:
- `users.all` will create a file at `.apify/users/all.json`
- `auth.login` will create a file at `.apify/auth/login.json`

The command will interactively prompt for:
1. API request name 
2. HTTP method (GET, POST, PUT, DELETE, etc.)
3. Endpoint URI
4. Optional: JSON payload for POST/PUT methods
5. Optional: HTTP headers
6. Optional: File uploads for multi-part form data
7. Optional: Test assertions

Example:
```bash
# Create a new API request
dotnet run create request --file users.all

# Create a request in a nested directory structure
dotnet run create request --file auth.login

# Force overwrite of an existing file
dotnet run create request --file users.all --force

# The .json extension is automatically added
```

#### `create mock` Command

Creates a new mock API response interactively.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--file` | The file path for the new mock API definition | Yes | - |
| `--force` | Force overwrite if the file already exists | No | false |
| `--debug` | Show detailed debug output and stack traces | No | false |

The file path supports dot notation for creating nested directories:
- `users.get` will create a file at `.apify/users/get.mock.json`
- `auth.login` will create a file at `.apify/auth/login.mock.json`

The command will interactively prompt for:
1. Mock API name
2. Endpoint path
3. HTTP method (GET, POST, PUT, DELETE, etc.)
4. Response status code
5. Content type
6. Response body
7. Optional: Custom response headers
8. Optional: Response delay (simulated latency)
9. Optional: Conditional responses based on request parameters

Example:
```bash
# Create a new mock API response
dotnet run create mock --file users.get

# Create a mock in a nested directory structure
dotnet run create mock --file auth.login

# Force overwrite of an existing file
dotnet run create mock --file users.get --force

# The .mock.json extension is automatically added
```

#### `mock-server` Command

Starts a local API mock server using mock definition files placed in the `.apify` directory.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--port` or `-p` | Port number to run the mock server on | No | 8080 |
| `--debug` | Show detailed debug output and stack traces | No | false |
| `--directory` or `-d` | Directory containing mock definitions | No | ".apify" |

The mock server provides a convenient way to test API interactions without requiring access to a real API. This is particularly useful for:

- Developing frontend applications that need API responses
- Testing edge cases and error scenarios
- Working offline without internet access
- Testing integration points before real APIs are available

Example:
```bash
# Start the mock server on the default port
dotnet run mock-server

# Start the mock server on a custom port
dotnet run mock-server --port 3000

# Start the mock server with debug information
dotnet run mock-server --port 3000 --debug

# Start the mock server using a specific directory
dotnet run mock-server --directory ./my-mocks
```

When running, the mock server listens for incoming HTTP requests and responds based on the mock definitions found in the specified directory. It displays a list of available endpoints on startup and logs requests it receives.

**Note**: The `--debug` flag provides detailed information including stack traces, request parsing details, and condition evaluation logic.

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
  }
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

## Payload Types

Apify supports multiple payload types for flexibility in testing different types of API endpoints. This section details all available payload types and their specific configurations.

| Payload Type | Description | Content-Type Header | Typical Use Cases |
|--------------|-------------|---------------------|-------------------|
| `none` | No request body | None | GET, DELETE requests |
| `json` | JSON structured data | application/json | Most modern REST APIs |
| `text` | Plain text content | text/plain | Simple text-based APIs |
| `formData` | URL-encoded form data | application/x-www-form-urlencoded | Traditional web forms, legacy APIs |

### None

Use this when no payload should be sent with the request. This is the default for GET, HEAD, and DELETE requests.

```json
"Payload": null,
"PayloadType": "none"
```

When using this payload type:
- No request body is sent
- No Content-Type header is automatically added
- Any Content-Type header you specify in the Headers section will still be included

### JSON

JSON is the most common payload type for modern REST APIs. The tool supports two ways to specify JSON payloads:

1. **Native JSON Objects** (recommended): The payload is specified as an actual JSON object, not a string:

```json
"Payload": {
  "name": "John Doe",
  "email": "john@example.com",
  "roles": ["admin", "user"],
  "profile": {
    "age": 30,
    "location": "New York"
  }
},
"PayloadType": "json"
```

2. **JSON as String** (legacy support): The payload is specified as a string containing JSON:

```json
"Payload": "{ \"name\": \"John Doe\", \"email\": \"john@example.com\" }",
"PayloadType": "json"
```

Key features of JSON payload type:
- The Content-Type header is automatically set to "application/json" if not otherwise specified
- Native JSON objects provide better type safety and avoid string escaping issues
- JSON objects can be arbitrarily complex with nested objects and arrays
- Variable substitution is supported in JSON string values (not in keys)

### Text

Use this type when you need to send plain text data in the request body:

```json
"Payload": "This is a text message",
"PayloadType": "text",
"Headers": {
  "Content-Type": "text/plain"
}
```

Key features of Text payload type:
- The Content-Type header is automatically set to "text/plain" if not otherwise specified
- Useful for APIs that expect simple string data
- Variable substitution works within the text string
- Appropriate for non-structured data or custom formats

### Form Data

Form Data is used for sending data that would traditionally be submitted by HTML forms. The Content-Type header is automatically set to "application/x-www-form-urlencoded".

```json
"Payload": {
  "username": "johndoe",
  "password": "secret",
  "remember": "true"
},
"PayloadType": "formData"
```

Key features of Form Data payload type:
- Data is sent as key-value pairs in URL-encoded format (e.g., `username=johndoe&password=secret`)
- Suitable for legacy APIs and endpoints that expect traditional form submissions
- More compact than JSON for simple key-value data
- Can be combined with file uploads (which automatically switches to multipart/form-data)
- Variable substitution works in both keys and values

When combined with file uploads, the content type automatically changes to multipart/form-data:

```json
"Payload": {
  "description": "Profile update"
},
"PayloadType": "formData",
"Files": [
  {
    "Name": "Profile Picture",
    "FieldName": "avatar",
    "FilePath": "./images/profile.jpg",
    "ContentType": "image/jpeg"
  }
]
```

## File Uploads

Apify supports file uploads using multipart/form-data:

```json
"Files": [
  {
    "Name": "Profile Picture",
    "FieldName": "avatar",
    "FilePath": "./images/profile.jpg",
    "ContentType": "image/jpeg"
  },
  {
    "Name": "Document",
    "FieldName": "document",
    "FilePath": "./docs/resume.pdf",
    "ContentType": "application/pdf"
  }
]
```

### File Upload Fields

| Field | Description | Required |
|-------|-------------|----------|
| `Name` | Descriptive name for the file | Yes |
| `FieldName` | Form field name for the file | Yes |
| `FilePath` | Path to the file on disk | Yes |
| `ContentType` | MIME type of the file | Yes |

When files are included, the request is automatically sent as multipart/form-data. You can also include form fields alongside files by providing a payload with `"PayloadType": "formData"`.

## Test Assertions

Apify provides comprehensive assertion capabilities to validate API responses. All test assertions support variable substitution in their parameters.

### Supported Assertion Types

| Assertion Type | Description | Required Parameters | Example Usage |
|----------------|-------------|---------------------|--------------|
| `StatusCode` | Validates the HTTP status code | `ExpectedValue` | Verify status 200, 201, 404, etc. |
| `ContainsProperty` | Checks if response JSON contains a specific property | `Property`, `ExpectedValue` | Check if "id", "name", or nested properties exist |
| `HeaderContains` | Validates a response header's value | `Property`, `ExpectedValue` | Check Content-Type, Cache-Control, etc. |
| `ResponseTimeBelow` | Verifies response time is below threshold | `ExpectedValue` | Ensure response is under specified milliseconds |
| `Equal` | Checks if a JSON property equals a specific value | `Property`, `ExpectedValue` | Verify exact field values |
| `IsArray` | Checks if a JSON property is an array | `Property` | Verify an array was returned |
| `ArrayNotEmpty` | Verifies a JSON array is not empty | `Property` | Ensure an array has at least one element |

### Status Code

Validates the HTTP status code returned by the API.

```json
{
  "Name": "Status code is OK",
  "Description": "Status code should be 200",
  "AssertType": "StatusCode",
  "ExpectedValue": "200"
}
```

This assertion passes if the API returns a status code that exactly matches the expected value. You can use variables in the `ExpectedValue` field, e.g., `"ExpectedValue": "{{expectedStatus}}"`.

You can also verify the status code falls within a valid range using custom expressions:

```json
{
  "Name": "Status code is success",
  "Description": "Status code should be in 2xx range",
  "AssertType": "StatusCode",
  "ExpectedValue": "2xx"
}
```

Where:
- `2xx` matches any status code between 200-299
- `4xx` matches any status code between 400-499
- `5xx` matches any status code between 500-599

### Contains Property

Validates that a JSON response contains a specific property. You can check for nested properties using dot notation.

```json
{
  "Name": "Has nested property",
  "Description": "Response should contain user address city",
  "AssertType": "ContainsProperty",
  "Property": "user.address.city",
  "ExpectedValue": ""
}
```

The property path follows dot notation to access nested objects. For example, `user.address.city` would look for the "city" property inside the "address" object, which is inside the "user" object.

### Header Contains

Validates that a response header contains a specific value.

```json
{
  "Name": "Content type is JSON",
  "Description": "Content-Type header should be application/json",
  "AssertType": "HeaderContains",
  "Property": "Content-Type",
  "ExpectedValue": "application/json"
}
```

This assertion checks if the specified header contains the expected value. Headers are matched case-insensitively.

### Response Time Below

Verifies that the API response time is below a specified threshold in milliseconds.

```json
{
  "Name": "Fast response",
  "Description": "Response time should be under 500ms",
  "AssertType": "ResponseTimeBelow",
  "ExpectedValue": "500"
}
```

This assertion passes if the response time is less than the specified value in milliseconds. You can use variables in the expected value, e.g., `"ExpectedValue": "{{maxResponseTime}}"`.

### Equal

Checks if a specific JSON property in the response equals an expected value.

```json
{
  "Name": "User ID matches",
  "Description": "User ID should be 12345",
  "AssertType": "Equal",
  "Property": "id",
  "ExpectedValue": "12345"
}
```

For nested properties, use dot notation:

```json
{
  "Name": "User city is correct",
  "Description": "User should be in New York",
  "AssertType": "Equal",
  "Property": "address.city",
  "ExpectedValue": "New York"
}
```

### IsArray

Checks if a JSON property in the response is an array.

```json
{
  "Name": "Results are an array",
  "Description": "Response should contain an array of results",
  "AssertType": "IsArray",
  "Property": "results"
}
```

For nested arrays, use dot notation:

```json
{
  "Name": "User has roles",
  "Description": "User should have a roles array",
  "AssertType": "IsArray",
  "Property": "user.roles"
}
```

### ArrayNotEmpty

Verifies that a JSON array in the response is not empty.

```json
{
  "Name": "Has at least one result",
  "Description": "Results array should not be empty",
  "AssertType": "ArrayNotEmpty",
  "Property": "results"
}
```

For nested arrays, use dot notation:

```json
{
  "Name": "User has at least one role",
  "Description": "User should have at least one role assigned",
  "AssertType": "ArrayNotEmpty",
  "Property": "user.roles"
}
```

## Configuration Properties

Apify uses a configuration file (`apify-config.json`) to store environment variables and project settings. The file is created automatically by the `init` command.

```json
{
  "Name": "My API Project",
  "Description": "Testing various API endpoints",
  "DefaultEnvironment": "Development",
  "Environments": [
    {
      "Name": "Development",
      "Description": "Local development environment",
      "Variables": {
        "baseUrl": "https://dev-api.example.com",
        "apiKey": "dev-key-123",
        "timeout": "5000"
      }
    },
    {
      "Name": "Production",
      "Description": "Production environment",
      "Variables": {
        "baseUrl": "https://api.example.com",
        "apiKey": "prod-key-456",
        "timeout": "3000"
      }
    }
  ]
}
```

### Configuration Fields

| Field | Description |
|-------|-------------|
| `Name` | Name of the API testing project |
| `Description` | Description of the project |
| `DefaultEnvironment` | The environment to use when none is specified |
| `Environments` | Array of environment configurations |

Each environment has:
- `Name`: Unique identifier for the environment
- `Description`: Optional description
- `Variables`: Key-value pairs of environment variables

## Variable System

Apify provides a powerful variable system that supports template substitution in API definitions. Variables can be defined at three levels, in order of precedence:

1. **Request-level variables** (highest priority) - defined in the API definition file
2. **Environment variables** (medium priority) - defined in the current environment
3. **Project-level variables** (lowest priority) - defined at the project level

Variables are referenced in API definitions using double curly braces:

```json
"Uri": "{{baseUrl}}/users/{{userId}}",
"Headers": {
  "Authorization": "Bearer {{token}}"
}
```

### Variable Precedence

If the same variable is defined at multiple levels, the request-level takes precedence over environment variables, which take precedence over project-level variables.

For example, if both the environment and the API definition file define a `userId` variable, the one from the API definition file will be used.

## Custom Variables

You can define custom variables directly in API definition files:

```json
{
  "Name": "Custom Variables Demo",
  "Uri": "{{baseUrl}}/api/products?apiKey={{apiKey}}&version={{version}}",
  "Method": "GET",
  "Variables": {
    "version": "2.0",
    "sessionId": "test-session-123",
    "customHeader": "X-Custom-Value"
  },
  "Headers": {
    "X-Session-ID": "{{sessionId}}",
    "{{customHeader}}": "CustomValue"
  }
}
```

Custom variables can be used in:
- URI paths and query parameters
- Headers (both keys and values)
- Payload data (both keys and values for structured data)
- Test assertions

## Mock Server

The mock server feature allows you to create simulated API responses for testing clients without requiring access to a real API. This is particularly useful when developing frontend applications or when the actual API is not yet available.

### Basic Mock Server

To create a basic mock API, create a file with the `.mock.json` extension in your `.apify` directory:

```json
{
  "Name": "Get User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/:id",
  "StatusCode": 200,
  "ContentType": "application/json",
  "Response": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com"
  }
}
```

Basic mock files support:
- Path parameters with colon notation (`:id`)
- Static JSON responses
- Custom HTTP status codes
- Custom response headers

### Advanced Mock Server

For more complex scenarios, Apify supports advanced mock definitions with conditional responses:

```json
{
  "Name": "User API",
  "Method": "GET",
  "Endpoint": "/api/users/:id",
  "Responses": [
    {
      "Condition": "q.id == \"1\"",
      "StatusCode": 200,
      "Response": {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com"
      }
    },
    {
      "Condition": "q.id == \"2\"",
      "StatusCode": 200,
      "Response": {
        "id": 2,
        "name": "Jane Smith",
        "email": "jane@example.com"
      }
    },
    {
      "Condition": "true",
      "StatusCode": 404,
      "Response": {
        "error": "User not found"
      }
    }
  ]
}
```

### Dynamic Responses

The mock server supports dynamic responses using variable substitution:

```json
{
  "Name": "Echo API",
  "Method": "POST",
  "Endpoint": "/api/echo",
  "StatusCode": 200,
  "ContentType": "application/json",
  "Response": {
    "message": "You sent: {{body.message}}",
    "timestamp": "{{datetime}}",
    "headers": "{{headers}}",
    "queryParams": "{{query}}"
  }
}
```

Available template variables:
- `{{body}}` - The parsed request body (JSON)
- `{{body.property}}` - A specific property from the request body
- `{{headers}}` - All request headers
- `{{headers.X-Custom-Header}}` - A specific request header
- `{{query}}` - All query parameters
- `{{query.param}}` - A specific query parameter
- `{{path}}` - The request path
- `{{timestamp}}` - The current Unix timestamp
- `{{datetime}}` - The current date and time (ISO format)
- `{{randomString}}` - A random string
- `{{randomInt}}` - A random integer

### Conditional Responses

The mock server can return different responses based on conditions. The conditions are JavaScript expressions that have access to request data:

```json
{
  "Name": "Conditional API",
  "Method": "GET",
  "Endpoint": "/api/items",
  "Responses": [
    {
      "Condition": "q.category == \"books\" && h[\"x-api-key\"] == \"valid-key\"",
      "StatusCode": 200,
      "Response": [
        { "id": 1, "name": "Book 1", "category": "books" },
        { "id": 2, "name": "Book 2", "category": "books" }
      ]
    },
    {
      "Condition": "q.category == \"electronics\"",
      "StatusCode": 200,
      "Response": [
        { "id": 3, "name": "Laptop", "category": "electronics" },
        { "id": 4, "name": "Phone", "category": "electronics" }
      ]
    },
    {
      "Condition": "h[\"x-api-key\"] !== \"valid-key\"",
      "StatusCode": 401,
      "Response": {
        "error": "Invalid API key"
      }
    },
    {
      "Condition": "true",
      "StatusCode": 200,
      "Response": []
    }
  ]
}
```

Condition syntax:
- `q` or `query` - Access query parameters: `q.paramName` or `query.paramName`
- `h` or `headers` - Access headers: `h["header-name"]` or `headers["header-name"]`
- `b` or `body` - Access the request body: `b.property` or `body.property`
- `p` or `path` - Access path parameters: `p.paramName` or `path.paramName`

Both dot notation and bracket notation are supported, with headers being case-insensitive.

## Examples

### Simple GET Request

```json
{
  "Name": "Get User",
  "Description": "Fetch a user by ID",
  "Uri": "{{baseUrl}}/users/1",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json"
  },
  "Tests": [
    {
      "Name": "Status is 200",
      "AssertType": "StatusCode",
      "ExpectedValue": "200"
    },
    {
      "Name": "Response has user ID",
      "AssertType": "ContainsProperty",
      "Property": "id",
      "ExpectedValue": ""
    }
  ]
}
```

### POST Request with JSON Payload

```json
{
  "Name": "Create User",
  "Description": "Create a new user",
  "Uri": "{{baseUrl}}/users",
  "Method": "POST",
  "Headers": {
    "Content-Type": "application/json",
    "Accept": "application/json"
  },
  "Payload": {
    "name": "John Doe",
    "email": "john@example.com",
    "role": "user"
  },
  "PayloadType": "json",
  "Tests": [
    {
      "Name": "Status is 201",
      "AssertType": "StatusCode",
      "ExpectedValue": "201"
    },
    {
      "Name": "Response has ID",
      "AssertType": "ContainsProperty",
      "Property": "id",
      "ExpectedValue": ""
    },
    {
      "Name": "Name is correct",
      "AssertType": "Equal",
      "Property": "name",
      "ExpectedValue": "John Doe"
    }
  ]
}
```

### File Upload Example

```json
{
  "Name": "Upload Profile Picture",
  "Description": "Upload a user profile picture",
  "Uri": "{{baseUrl}}/users/{{userId}}/avatar",
  "Method": "POST",
  "Headers": {
    "Authorization": "Bearer {{token}}"
  },
  "Payload": {
    "description": "Profile photo"
  },
  "PayloadType": "formData",
  "Files": [
    {
      "Name": "Profile Image",
      "FieldName": "avatar",
      "FilePath": "./images/profile.jpg",
      "ContentType": "image/jpeg"
    }
  ],
  "Tests": [
    {
      "Name": "Status is 200",
      "AssertType": "StatusCode",
      "ExpectedValue": "200"
    },
    {
      "Name": "Upload successful",
      "AssertType": "ContainsProperty",
      "Property": "success",
      "ExpectedValue": "true"
    }
  ]
}
```

### Mock Server Example

```json
{
  "Name": "User Authentication",
  "Method": "POST",
  "Endpoint": "/api/auth/login",
  "RequireAuthentication": false,
  "Responses": [
    {
      "Condition": "b.username == \"admin\" && b.password == \"admin123\"",
      "StatusCode": 200,
      "Response": {
        "success": true,
        "token": "admin-token-123",
        "user": {
          "id": 1,
          "username": "admin",
          "role": "admin"
        }
      }
    },
    {
      "Condition": "b.username == \"user\" && b.password == \"user123\"",
      "StatusCode": 200,
      "Response": {
        "success": true,
        "token": "user-token-456",
        "user": {
          "id": 2,
          "username": "user",
          "role": "user"
        }
      }
    },
    {
      "Condition": "true",
      "StatusCode": 401,
      "Response": {
        "success": false,
        "error": "Invalid username or password"
      }
    }
  ]
}
```

## Troubleshooting

### Common Issues

#### Request Failures

If your request fails with connection errors:

1. **Check the base URL** - Ensure the base URL is correct and includes the protocol (http:// or https://)
2. **Verify network connectivity** - Make sure you can reach the API server from your machine
3. **Check for timeouts** - For slow APIs, try increasing the timeout value
4. **Debug with verbose mode** - Use the `--verbose` flag to see detailed request information
5. **Examine detailed errors** - Use the `--debug` flag to see stack traces and detailed error information

#### Variable Substitution

If variables aren't being substituted:

1. **Check variable names** - Ensure the variable names match exactly (case-sensitive)
2. **Verify environment** - Check which environment is active with `list-env`
3. **Variable format** - Make sure you're using double curly braces: `{{variableName}}`
4. **Check precedence** - Remember that request-level variables override environment variables

#### Mock Server Issues

Common mock server problems:

1. **Port already in use** - Try a different port with `--port`
2. **Access denied on Windows** - Run as Administrator or use a port above 1024
3. **Endpoint not matching** - Check that the URL path exactly matches the defined endpoint
4. **Condition not matching** - Use the `--debug` flag to see condition evaluation details
5. **Case sensitivity** - Paths and query parameters are case-sensitive by default

#### Authentication Problems

If you're having authentication issues:

1. **Check credentials** - Verify API keys, tokens, and credentials are correct
2. **Header format** - Ensure the Authorization header is formatted correctly
3. **Token expiration** - Check if your token or API key has expired
4. **Debug request** - Use `--verbose` to see exactly what's being sent

### Getting Support

If you continue to experience issues:

1. Check the GitHub repository for known issues
2. Review examples in the `.apify/Examples` directory
3. Debug API responses with `--verbose` and `--debug` flags
4. Create a detailed bug report with reproduction steps if needed