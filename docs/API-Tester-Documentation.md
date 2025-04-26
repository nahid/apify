# API Tester Documentation

## Overview

API Tester is a robust command-line tool designed for API testing and validation. It allows developers to define API tests in JSON format and execute them against endpoints, providing detailed output of the request, response, and test results. The tool offers environment management, variable substitution, and support for various payload types and file uploads.

## Table of Contents

1. [Installation](#installation)
2. [Getting Started](#getting-started)
3. [Command Reference](#command-reference)
4. [API Test Definition](#api-test-definition)
5. [Payload Types](#payload-types)
6. [File Uploads](#file-uploads)
7. [Test Assertions](#test-assertions)
8. [Environment Variables](#environment-variables)
9. [Examples](#examples)
10. [Troubleshooting](#troubleshooting)

## Installation

To use API Tester, you need to have .NET 8.0 SDK installed on your system.

```bash
# Clone the repository
git clone https://github.com/yourusername/api-tester.git
cd api-tester

# Build the project
dotnet build

# Run the application
dotnet run
```

## Getting Started

### Initializing a New Project

To create a new API testing project with default configuration:

```bash
dotnet run init --name "My API Project" --base-url "https://api.example.com" 
```

This will:
- Create a `apis` directory to store your API test definitions
- Generate a default configuration file `apify-config.json` with development and production environments
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
| `list-env` | List available environments |

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

#### `list-env` Command

Lists all available environments and their variables.

```bash
dotnet run list-env
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

## Payload Types

API Tester supports multiple payload types for flexibility in testing different types of API endpoints:

### None

No payload is sent with the request. Typically used for GET requests.

```json
"Payload": null,
"PayloadType": "none"
```

### JSON

The tool supports two ways to specify JSON payloads:

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

Both approaches are supported, but using native JSON objects is recommended as it provides better type safety and avoids string escaping issues. The Content-Type header is automatically set to "application/json" in both cases.

### Text

Plain text payload. The Content-Type header is automatically set to "text/plain".

```json
"Payload": "This is a text message",
"PayloadType": "text",
"Headers": {
  "Content-Type": "text/plain"
}
```

### Form Data

URL encoded form data. The Content-Type header is automatically set to "application/x-www-form-urlencoded".

```json
"Payload": {
  "username": "johndoe",
  "password": "secret"
},
"PayloadType": "formData"
```

## File Uploads

API Tester supports file uploads using multipart/form-data:

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

API Tester provides various assertion types to validate API responses:

### Status Code

Validates the HTTP status code:

```json
{
  "Name": "Status code is OK",
  "Description": "Status code should be 200",
  "AssertType": "StatusCode",
  "ExpectedValue": "200"
}
```

### Contains Property

Validates that a JSON response contains a specific property:

```json
{
  "Name": "User ID exists",
  "Description": "Response should contain a user ID",
  "AssertType": "ContainsProperty",
  "ExpectedValue": "id"
}
```

### Header Contains

Validates that a response header contains a specific value:

```json
{
  "Name": "Content type is JSON",
  "Description": "Content-Type header should be application/json",
  "AssertType": "HeaderContains",
  "Property": "Content-Type",
  "ExpectedValue": "application/json"
}
```

### Response Time Below

Validates that the response time is below a specific threshold:

```json
{
  "Name": "Response time is acceptable",
  "Description": "Response time should be under 500ms",
  "AssertType": "ResponseTimeBelow",
  "ExpectedValue": "500"
}
```

## Environment Variables

Environment variables allow you to customize your API tests for different environments (development, staging, production, etc.).

### Configuration File

The environment configuration is stored in `apify-config.json`:

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
  "DefaultEnvironment": "Development"
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

### Debug Options

For more detailed information about request and response, use the verbose flag:

```bash
dotnet run run apis/user-api.json --verbose
```

This will show:
- Full request details including headers and payload
- Complete response with headers and body
- Detailed test results with error messages for failed tests