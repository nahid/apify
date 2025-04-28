# Apify Documentation

## Overview

Apify is a robust command-line tool designed for API testing and validation. It allows developers to define API tests in JSON format and execute them against endpoints, providing detailed output of the request, response, and test results. The tool offers centralized environment management, variable substitution, and support for various payload types and file uploads.

## Table of Contents

1. [Installation](#installation)
2. [Getting Started](#getting-started)
3. [Command Reference](#command-reference)
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
- Create a `apis` directory to store your API test definitions
- Generate a configuration file `apify-config.json` with development and production environments
- Create sample API test files in the `apis` directory

### Running Your First Test

After initialization, you can run the sample test:

```bash
dotnet run run apis/sample-api.json
```

For more detailed output, use the verbose flag:

```bash
dotnet run run apis/sample-api.json --verbose
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

### Command Options

#### `init` Command

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--name` | The name of the API testing project | Yes | - |
| `--base-url` | The base URL for API endpoints | Yes | - |
| `--environment` | The default environment | No | "Development" |
| `--force` | Force overwrite of existing files | No | false |

Example:
```bash
dotnet run init --name "Payment API Tests" --base-url "https://payment.api.example.com" --environment "Staging" --force
```

#### `run` Command

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `files` | API definition files to test (supports wildcards) | Yes | - |
| `--verbose` or `-v` | Display detailed output | No | false |
| `--profile` or `-p` | Configuration profile to use | No | "Default" |
| `--env` or `-e` | Environment to use from the profile | No | Profile's default |

Examples:
```bash
# Run a single test
dotnet run run apis/user-api.json

# Run all tests in the apis directory
dotnet run run apis/*.json --verbose

# Run tests using a specific environment
dotnet run run apis/payment-api.json --env Production
```

#### `tests` Command

The tests command scans the project directory and runs all API tests found in the `.apify` directory and its subdirectories, providing a visual progress display during execution.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--verbose` or `-v` | Display detailed output including response body | No | false |
| `--env` or `-e` | Environment to use from the profile | No | Profile's default |
| `--tag` | Filter tests by tag | No | - |

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
dotnet run tests --verbose

# Run only tests with a specific tag
dotnet run tests --tag payments
```

#### `list-env` Command

Lists all available environments and their variables.

```bash
dotnet run list-env
```

#### `create request` Command

Creates a new API request definition interactively.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--file` | The file path for the new API request definition | Yes | - |

The file path supports dot notation for creating nested directories:
- `users.all` will create a file at `.apify/users/all.json`
- `auth.login` will create a file at `.apify/auth/login.json`

The command will interactively prompt for:
1. API request name 
2. HTTP method (GET, POST, PUT, DELETE, etc.)
3. Endpoint URI
4. Optional: JSON payload for POST/PUT methods
5. Optional: HTTP headers

Example:
```bash
# Create a new API request
dotnet run create request --file users.all

# Create a request in a nested directory structure
dotnet run create request --file auth.login

# The .json extension is automatically added
```

#### `mock-server` Command

Starts a local API mock server using mock definition files placed in the `.apify` directory.

| Option | Description | Required | Default |
|--------|-------------|----------|---------|
| `--port` or `-p` | Port number to run the mock server on | No | 8080 |
| `--verbose` or `-v` | Enable verbose logging for debugging | No | false |
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

# Start the mock server on a custom port with verbose logging
dotnet run mock-server --port 3000 --verbose

# Start the mock server using a specific directory
dotnet run mock-server --directory ./my-mocks
```

When running, the mock server listens for incoming HTTP requests and responds based on the mock definitions found in the specified directory. It displays a list of available endpoints on startup and logs requests it receives.

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
  "Timeout": 30000
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
| `ContainsProperty` | Checks if response JSON contains a specific property | `ExpectedValue` | Check if "id", "name", or nested properties exist |
| `HeaderContains` | Validates a response header's value | `Property`, `ExpectedValue` | Check Content-Type, Cache-Control, etc. |
| `ResponseTimeBelow` | Verifies response time is below threshold | `ExpectedValue` | Ensure response is under specified milliseconds |
| `Equal` | Checks if a JSON property equals a specific value | `PropertyPath`, `ExpectedValue` | Verify exact field values |

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

### Contains Property

Validates that a JSON response contains a specific property at any level in the hierarchy.

```json
{
  "Name": "User ID exists",
  "Description": "Response should contain a user ID",
  "AssertType": "ContainsProperty",
  "ExpectedValue": "id"
}
```

The assertion searches the entire JSON object recursively, so it will find the property even if it's nested inside arrays or other objects. This is useful for verifying that required fields are present in the response.

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

* `Property`: The name of the header to check (case-insensitive)
* `ExpectedValue`: The value that should be contained within the header value

The assertion passes if the specified header contains the expected value as a substring. This allows for partial matching, which is useful for headers like Content-Type where the value might include additional parameters (e.g., "application/json; charset=utf-8").

### Response Time Below

Validates that the response time is below a specific threshold in milliseconds.

```json
{
  "Name": "Response time is acceptable",
  "Description": "Response time should be under 500ms",
  "AssertType": "ResponseTimeBelow",
  "ExpectedValue": "500"
}
```

This assertion is useful for performance testing and ensuring that your API meets response time requirements. The response time is measured from when the request is sent until the full response is received.

### Equal

Validates that a specific property in the JSON response has an exact value.

```json
{
  "Name": "Check user ID value",
  "Description": "Verifies the user ID is correct",
  "AssertType": "Equal",
  "PropertyPath": "id",
  "ExpectedValue": "123"
}
```

* `PropertyPath`: The path to the property to check (supports dot notation for nested properties, e.g., "user.address.city")
* `ExpectedValue`: The exact value the property should have

For nested properties, you can use dot notation:

```json
{
  "Name": "Check nested value",
  "Description": "Verifies a nested property value",
  "AssertType": "Equal",
  "PropertyPath": "user.profile.settings.theme",
  "ExpectedValue": "dark"
}
```

Variables can be used in both the `PropertyPath` and `ExpectedValue` fields, making this assertion type very flexible.

## Configuration Properties

This section details all the configuration properties used in the Apify, both for the global configuration file (`apify-config.json`) and individual API test files.

### Configuration File Properties

The main configuration file (`apify-config.json`) contains the following properties:

| Property | Description | Required | Type |
|----------|-------------|----------|------|
| `Name` | The name of the configuration profile | Yes | String |
| `Description` | A description of what this configuration is for | No | String |
| `Environments` | An array of environment configurations | Yes | Array of Environment objects |
| `DefaultEnvironment` | The name of the default environment to use | Yes | String |
| `Variables` | Project-level variables that apply across all environments | No | Object (key-value pairs) |

#### Environment Object Properties

Each environment in the `Environments` array contains:

| Property | Description | Required | Type |
|----------|-------------|----------|------|
| `Name` | The name of the environment (e.g., "Development", "Production") | Yes | String |
| `Description` | A description of the environment | No | String |
| `Variables` | Environment-specific variables | Yes | Object (key-value pairs) |

### API Test File Properties

Each API test is defined in a JSON file with the following properties:

| Property | Description | Required | Type | Default |
|----------|-------------|----------|------|---------|
| `Name` | The name of the API test | Yes | String | - |
| `Description` | A description of what this test does | No | String | - |
| `Uri` | The endpoint URI (supports variable substitution) | Yes | String | - |
| `Method` | HTTP method (GET, POST, PUT, DELETE, etc.) | Yes | String | "GET" |
| `Headers` | HTTP headers as key-value pairs | No | Object | null |
| `Payload` | The request body (can be JSON object, string, etc.) | No | Object/String | null |
| `PayloadType` | Type of payload (none, json, text, formData) | No | String | "json" |
| `Files` | Files to upload (for multipart/form-data) | No | Array of File objects | null |
| `Tests` | Test assertions to validate the response | No | Array of Test objects | null |
| `Timeout` | Request timeout in milliseconds | No | Number | 30000 |
| `Variables` | Custom variables defined for this test | No | Object | null |

#### File Object Properties

Each file in the `Files` array contains:

| Property | Description | Required | Type |
|----------|-------------|----------|------|
| `Name` | Descriptive name for the file | Yes | String |
| `FieldName` | Form field name for the file | Yes | String |
| `FilePath` | Path to the file on disk | Yes | String |
| `ContentType` | MIME type of the file | Yes | String |

#### Test Object Properties

Each test in the `Tests` array contains:

| Property | Description | Required | Type |
|----------|-------------|----------|------|
| `Name` | Name of the test | Yes | String |
| `Description` | Description of what the test validates | No | String |
| `AssertType` | Type of assertion (StatusCode, ContainsProperty, etc.) | Yes | String |
| `PropertyPath` | Path to the property to check (for Equal assertions) | Required for Equal | String |
| `Property` | Header name (for HeaderContains) | Required for HeaderContains | String |
| `ExpectedValue` | The expected value to check against | Yes | String |

## Variable System

Apify provides a flexible variable system with three levels of variables: project-level, environment-specific, and request-specific variables.

### Configuration File

The configuration is stored in `apify-config.json`:

```json
{
  "Name": "Default",
  "Description": "Default configuration profile",
  "Environments": [
    {
      "Name": "Development",
      "Description": "Development environment",
      "Variables": {
        "baseUrl": "https://dev-api.example.com",
        "token": "dev-token-123",
        "timeout": "5000"
      }
    },
    {
      "Name": "Production",
      "Description": "Production environment",
      "Variables": {
        "baseUrl": "https://api.example.com",
        "token": "prod-token-456",
        "timeout": "3000"
      }
    }
  ],
  "DefaultEnvironment": "Development",
  "Variables": {
    "projectId": "test-api-project",
    "version": "1.0.0",
    "apiVersion": "v1"
  }
}
```

### Variable Substitution

Environment variables can be used in API test definitions by enclosing them in double curly braces:

```json
"Uri": "{{baseUrl}}/users/{{userId}}",
"Headers": {
  "Authorization": "Bearer {{token}}"
}
```

## Custom Variables

In addition to environment variables, Apify allows defining custom variables directly within each API test definition file. These custom variables provide more flexibility and portability for your test files.

### Defining Custom Variables

Custom variables are defined in the "Variables" section of the API test definition:

```json
{
  "Name": "Custom Variables Test",
  "Uri": "{{baseUrl}}/users/{{userId}}",
  "Method": "GET",
  "Headers": {
    "Accept": "{{acceptHeader}}",
    "X-Custom-Header": "{{customValue}}"
  },
  "Tests": [
    {
      "Name": "Status code check",
      "Description": "Status code should be {{expectedStatus}}",
      "AssertType": "StatusCode",
      "ExpectedValue": "{{expectedStatus}}"
    }
  ],
  "Variables": {
    "userId": "123",
    "acceptHeader": "application/json",
    "customValue": "custom-header-value",
    "expectedStatus": "200"
  }
}
```

### Variable Priority System

The Apify implements a hierarchical variable priority system:

1. **Request-specific variables** (Highest priority)
   - Defined in the "Variables" section of each API test file
   - Override both environment variables and project-level variables
   - Specific to a single API test

2. **Environment variables** (Medium priority)
   - Defined in the environment sections of the apify-config.json
   - Override project-level variables
   - Environment-specific (Development, Production, etc.)

3. **Project-level variables** (Lowest priority)
   - Defined at the root level "Variables" section in apify-config.json
   - Shared across all environments
   - Provide baseline values for the entire project

When the tool encounters variables with the same name at different levels, it follows this priority order. For example, if a variable named "timeout" is defined in all three levels, the request-specific value will be used.

This hierarchical system allows you to:
1. Define project-wide settings at the project level
2. Override them with environment-specific values
3. Further customize them for specific API tests

### Benefits of Custom Variables

* **Test-Specific Values**: Define values that only apply to a specific test
* **Portability**: Tests can be shared without requiring environment setup
* **Testing Different Scenarios**: Easily change test behavior by modifying variables
* **Default Values**: Provide fallbacks even if environment variables are missing
* **Self-Documentation**: Variables defined in the test show what values the test expects

## Mock Server

The Mock Server is a powerful feature that allows you to create virtual API endpoints without needing access to real backend services. This is particularly valuable for:

1. Frontend development when the backend is not ready
2. Testing error scenarios and edge cases
3. Offline development
4. Creating reproducible test environments

### Mock Definition Files

Mock API definitions are JSON files with a `.mock.json` extension placed in the `.apify` directory. The naming convention is:

```
<endpoint-name>.mock.json
```

For example:
- `.apify/users/all.mock.json` - Mocks a "Get All Users" endpoint
- `.apify/users/user.mock.json` - Mocks a "Get User by ID" endpoint

### Mock Definition Structure

A mock definition file follows this structure:

```json
{
  "Name": "Get User by ID",
  "Description": "Returns a single user by ID",
  "Method": "GET",
  "Path": "/users/:id",
  "ResponseStatus": 200,
  "ResponseHeaders": {
    "Content-Type": "application/json",
    "Cache-Control": "max-age=3600"
  },
  "ResponseBody": {
    "id": "{{:id}}",
    "name": "John Doe",
    "email": "john@example.com",
    "isActive": true
  },
  "Conditions": [
    {
      "Type": "HeaderContains",
      "Field": "Authorization",
      "Value": "Bearer invalid",
      "ResponseStatus": 401,
      "ResponseHeaders": {
        "Content-Type": "application/json"
      },
      "ResponseBody": {
        "error": "Invalid authentication token"
      }
    }
  ]
}
```

### Fields Explained

| Field | Type | Description | Required |
|-------|------|-------------|----------|
| `Name` | String | Name of the mock endpoint | Yes |
| `Description` | String | Description of the endpoint | No |
| `Method` | String | HTTP method (GET, POST, PUT, DELETE, etc.) | Yes |
| `Path` | String | The endpoint path pattern, supporting route parameters (e.g., `/users/:id`) | Yes |
| `ResponseStatus` | Number | Default HTTP status code to return | Yes |
| `ResponseHeaders` | Object | Default HTTP headers to include in the response | No |
| `ResponseBody` | Object/Array/String | Default response body to return | No |
| `Conditions` | Array | Conditional response configurations | No |

### Path Parameters

The mock server supports path parameters in the URL pattern through the `:parameter` syntax:

```json
"Path": "/users/:id"
```

When a request is made to `/users/123`, the value `123` is captured as the `:id` parameter. This value can be used in the response with the `{{:id}}` template syntax:

```json
"ResponseBody": {
  "id": "{{:id}}",
  "name": "John Doe"
}
```

This allows you to create dynamic responses that reflect the request parameters.

### Conditional Responses

The `Conditions` array allows you to define different responses based on request properties. Each condition is evaluated in order, and the first matching condition's response is used.

#### Available Condition Types

| Condition Type | Description | Fields |
|----------------|-------------|--------|
| `HeaderContains` | Checks if a request header contains a value | `Field`, `Value` |
| `BodyContains` | Checks if the request body contains a value | `Value` |
| `BodyPropertyEquals` | Checks if a JSON property in the request equals a value | `PropertyPath`, `Value` |
| `QueryStringContains` | Checks if a query parameter contains a value | `Field`, `Value` |
| `PathParameterEquals` | Checks if a path parameter equals a value | `Parameter`, `Value` |

Example of conditional responses:

```json
"Conditions": [
  {
    "Type": "HeaderContains",
    "Field": "Authorization",
    "Value": "Bearer invalid",
    "ResponseStatus": 401,
    "ResponseBody": {
      "error": "Invalid token"
    }
  },
  {
    "Type": "BodyPropertyEquals",
    "PropertyPath": "user.role",
    "Value": "admin",
    "ResponseStatus": 200,
    "ResponseBody": {
      "message": "Admin access granted"
    }
  }
]
```

### Dynamic Response Templates

The mock server supports dynamic content generation in responses using template variables:

| Template Variable | Description | Example |
|-------------------|-------------|---------|
| `{{$timestamp}}` | Current Unix timestamp | 1618435200 |
| `{{$date}}` | Current date in ISO format | 2023-04-15T12:00:00Z |
| `{{$random:int:min:max}}` | Random integer between min and max | {{$random:int:1:100}} |
| `{{$random:uuid}}` | Random UUID | 123e4567-e89b-12d3-a456-426614174000 |
| `{{$random:string:length}}` | Random alphanumeric string | {{$random:string:10}} |
| `{{:paramName}}` | Path parameter value | {{:id}} |

Example of using dynamic template variables:

```json
"ResponseBody": {
  "id": "{{:id}}",
  "name": "Jane Doe",
  "createdAt": "{{$date}}",
  "token": "{{$random:uuid}}",
  "randomCode": "{{$random:string:8}}"
}
```

### File Upload Handling

The mock server can handle file uploads by storing them temporarily and providing details about the uploaded files in the response:

```json
"ResponseBody": {
  "message": "File uploaded successfully",
  "fileDetails": "{{$uploadedFiles}}"
}
```

The `{{$uploadedFiles}}` template variable is replaced with information about all files uploaded in the request.

### Running the Mock Server

To start the mock server:

```bash
dotnet run mock-server --port 8080 --verbose
```

The mock server will display available endpoints on startup and log incoming requests, making it easy to debug your application's interactions with the API.

## Examples

### Basic GET Request

```json
{
  "Name": "Get User API Test",
  "Uri": "{{baseUrl}}/users/1",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json"
  },
  "Tests": [
    {
      "Name": "Status code is successful",
      "Description": "Status code is 200",
      "AssertType": "StatusCode",
      "ExpectedValue": "200"
    },
    {
      "Name": "Response contains user data",
      "Description": "Response contains user id",
      "AssertType": "ContainsProperty",
      "ExpectedValue": "id"
    }
  ]
}
```

### GET Request with Custom Variables

```json
{
  "Name": "Get Post with Custom Variables",
  "Uri": "{{baseUrl}}/posts/{{postId}}",
  "Method": "GET",
  "Headers": {
    "Accept": "{{acceptHeader}}",
    "X-Test-Header": "{{customHeaderValue}}"
  },
  "Tests": [
    {
      "Name": "Status code should be {{expectedStatus}}",
      "Description": "Checks if the status code matches the expected value",
      "AssertType": "StatusCode",
      "ExpectedValue": "{{expectedStatus}}"
    },
    {
      "Name": "Response contains Content-Type with {{contentType}}",
      "Description": "Validates that the response has the correct content type",
      "AssertType": "HeaderContains",
      "Property": "Content-Type",
      "ExpectedValue": "{{contentType}}"
    }
  ],
  "Variables": {
    "postId": "1",
    "acceptHeader": "application/json",
    "customHeaderValue": "custom-header-test-value",
    "expectedStatus": "200",
    "contentType": "application/json",
    "timeout": "15000"
  },
  "Timeout": 15000
}
```

### POST Request with JSON Payload

```json
{
  "Name": "Create User Test",
  "Uri": "{{baseUrl}}/users",
  "Method": "POST",
  "Headers": {
    "Accept": "application/json",
    "Content-Type": "application/json"
  },
  "Payload": {
    "name": "John Doe",
    "email": "john@example.com",
    "role": "admin"
  },
  "PayloadType": "json",
  "Tests": [
    {
      "Name": "Status code is created",
      "Description": "Status code is 201",
      "AssertType": "StatusCode",
      "ExpectedValue": "201"
    },
    {
      "Name": "Response contains user ID",
      "Description": "Response contains id property",
      "AssertType": "ContainsProperty",
      "ExpectedValue": "id"
    }
  ]
}
```

### File Upload Example

```json
{
  "Name": "User Profile Update",
  "Uri": "{{baseUrl}}/users/{{userId}}/profile",
  "Method": "PUT",
  "Headers": {
    "Accept": "application/json"
  },
  "Payload": "{ \"description\": \"Updated profile information\" }",
  "PayloadType": "formData",
  "Files": [
    {
      "Name": "Profile Picture",
      "FieldName": "avatar",
      "FilePath": "./images/profile.jpg",
      "ContentType": "image/jpeg"
    }
  ],
  "Tests": [
    {
      "Name": "Status code is successful",
      "Description": "Status code is 200",
      "AssertType": "StatusCode",
      "ExpectedValue": "200"
    }
  ]
}
```

## Troubleshooting

### Common Issues

1. **API endpoint not reachable:**
   - Check if the base URL is correct
   - Verify that the environment variables are properly set
   - Ensure you have internet connectivity

2. **Authentication failures:**
   - Verify that the authentication token is valid and correctly formatted
   - Check that the token is being properly substituted in the request

3. **File upload issues:**
   - Ensure the file exists at the specified path
   - Verify that the content type is correct for the file
   
4. **Variable substitution problems:**
   - Check project-level variables in the root "Variables" section of apify-config.json
   - Verify environment variables in each environment section of apify-config.json
   - For request-specific variables, check they are properly defined in the test file's "Variables" section
   - Remember the priority order: request-specific > environment > project-level variables
   - Ensure variable names match exactly (they are case-sensitive)

### Debug Options

For more detailed information about request and response, use the verbose flag:

```bash
dotnet run run apis/user-api.json --verbose
```

This will show:
- Full request details including headers and payload
- Complete response with headers and body
- Detailed test results with error messages for failed tests