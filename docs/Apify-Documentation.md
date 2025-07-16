# Apify

A powerful C# CLI application for comprehensive API testing and mocking, enabling developers to streamline API validation and development workflows with rich configuration and execution capabilities.

## Features

- **Comprehensive API Testing**: Define and run detailed API tests.
    - **Multiple Request Methods**: Support for GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS.
    - **Rich Payload Types**: JSON, Text, Form Data.
    - **File Upload Support**: Test multipart/form-data requests with file uploads.
    - **Detailed Assertions**: Validate response status, headers, body content (JSON properties, arrays, values), and response time.
- **Integrated Mock Server**: Simulate API endpoints for development and testing.
    - **Dynamic & Conditional Responses**: Define mock responses based on request parameters, headers, or body content.
    - **Template Variables**: Use built-in (random data, timestamps) and custom (request-derived) variables in mock responses.
    - **File-based Configuration**: Manage mock definitions in simple `.mock.json` files.
- **Environment Management**: Use different configurations for development, staging, production, etc.
    - **Variable Overriding**: Project, Environment, and Request-level variable support with clear precedence.
- **User-Friendly CLI**:
    - **Interactive Creation**: Commands to interactively create test and mock definitions.
    - **Detailed Reports**: Comprehensive output with request, response, and assertion details.
    - **Visual Progress Indicators**: Animated progress display for running multiple tests.
- **Deployment & Extensibility**:
    - **Single File Deployment**: Simplified deployment as a single executable file.
    - **.NET 8.0 & .NET 9 Ready**: Built with .NET 8.0, with automatic multi-targeting for .NET 9.0 when available.

## Getting Started

### Prerequisites

- .NET 8.0 SDK (required)
- .NET 9.0 SDK (optional, for building with .NET 9.0 when available)

### Installation

The easiest way to get Apify is to download the pre-built executable from the [GitHub Releases](https://github.com/nahid/apify/releases) page.

1.  Go to the [latest release](https://github.com/nahid/apify/releases/latest).
2.  Download the appropriate `.zip` file for your operating system and architecture (e.g., `apify-win-x64.zip` for Windows 64-bit, `apify-linux-x64.zip` for Linux 64-bit, `apify-osx-arm64.zip` for macOS ARM64).
3.  Extract the contents of the `.zip` file to a directory of your choice (e.g., `C:\Program Files\Apify` on Windows, `/opt/apify` on Linux/macOS).
4.  Add the directory where you extracted Apify to your system's PATH environment variable. This allows you to run `apify` from any terminal.

Alternatively, you can build Apify from source:

### Build from Source

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



## Core Concepts

### Project Initialization (`apify init`)

To start using Apify in your project, navigate to your project's root directory and run:

```bash
apify init
```

This command interactively prompts for:
- Project Name
- Default Environment Name (e.g., "Development")
- Additional environments (e.g., "Staging,Production")

It creates:
- `apify-config.json`: The main configuration file.
- `.apify/`: A directory to store your API test definitions (`.json`) and mock definitions (`.mock.json`).
- Sample API test and mock definition files within `.apify/` and `.apify/mocks/` respectively.
- A `MockServer` configuration block in `apify-config.json`.

### Configuration (`apify-config.json`)

This file stores project-level settings, environments, and mock server configuration.

```json
{
  "Name": "My Project API Tests",
  "Description": "API Tests for My Project",
  "DefaultEnvironment": "Development",
  "Variables": {
    "globalProjectVar": "This variable is available in all environments and tests"
  },
  "Environments": [
    {
      "Name": "Development",
      "Description": "Development environment specific variables",
      "Variables": {
        "baseUrl": "https://dev-api.myproject.com",
        "apiKey": "dev-secret-key"
      }
    },
    {
      "Name": "Production",
      "Description": "Production environment specific variables",
      "Variables": {
        "baseUrl": "https://api.myproject.com",
        "apiKey": "prod-secret-key"
      }
    }
  ],
  "MockServer": {
    "Port": 8080,
    "Directory": ".apify/mocks",
    "Verbose": false,
    "EnableCors": true,
    "DefaultHeaders": {
      "X-Mock-Server": "Apify"
    }
  }
}
```

- **`Name`, `Description`**: Project metadata.
- **`DefaultEnvironment`**: The environment used if `--env` is not specified.
- **`Variables` (Project-Level)**: Key-value pairs available across all tests and environments unless overridden.
- **`Environments`**: An array of environment objects.
    - **`Name`**: Unique name for the environment (e.g., "Development", "Staging").
    - **`Variables`**: Key-value pairs specific to this environment. These override project-level variables.
- **`MockServer`**: Configuration for the mock server.
    - **`Port`**: Port for the mock server.
    - **`Verbose`**: Enable verbose logging for the mock server.
    - **`EnableCors`**: Enable CORS headers (defaults to allow all).
    - **`DefaultHeaders`**: Headers to be added to all mock responses.

### API Test Definitions (`.json`)

API tests are defined in JSON files (e.g., `.apify/users/get-users.json`).

Structure:
```json
{
  "Name": "Get All Users",
  "Description": "Fetches the list of all users",
  "Url": "{{baseUrl}}/users?page={{defaultPage}}",
  "Method": "GET",
  "Headers": {
    "Accept": "application/json",
    "X-Api-Key": "{{apiKey}}"
  },
  "Body": null,
  "PayloadType": "none", // "json", "text", "formData"
  "Timeout": 30000, // Optional, in milliseconds
  "Tags": ["users", "smoke"], // Optional, for filtering tests
  "Variables": { // Request-specific variables (highest precedence)
    "defaultPage": "1"
  },
  "Tests": [
    {
      "Title": "Status code is 200 OK",
      "Case": "Assert.Response.StatusCodeIs(200)"
    },
    {
      "Title": "Response body is an array",
      "Case": "Assert.Response.Json.data.Type == JTokenType.Array"
    }
  ]
}
```

- **`Variables` (Request-Level)**: Override environment and project variables.
- **`Tags`**: Used for filtering tests with `apify tests --tag <tagname>`.
- **`Tests` (Assertions)**: A list of assertion objects.
    - **`Title`**: A descriptive name for the assertion.
    - **`Case`**: A C# expression to be evaluated. The expression should return a boolean value. You can use the `Assert` object and its methods to perform assertions.

### Mock API Definitions (`.mock.json`)

Mock APIs are defined in `.mock.json` files (e.g., `.apify/mocks/users/get-user-by-id.mock.json`).

Structure:
```json
{
  "Name": "Mock User by ID",
  "Method": "GET",
  "Endpoint": "/api/users/:id", // Path parameters with :param or {param}
  "Responses": [
    {
      "Condition": "path.id == \"1\"", // C#-like condition
      "StatusCode": 200,
      "Headers": {
        "X-Source": "Mock-Conditional-User1"
      },
      "ResponseTemplate": {
        "id": 1,
        "name": "John Doe (Mocked)",
        "email": "john.mock@example.com",
        "requested_id": "{{path.id}}",
        "random_code": "{{expr|> Faker.Random.Int(1000,9999)}}"
      }
    },
    {
      "Condition": "query.type == \"admin\" && header.X-Admin-Token == \"SUPER_SECRET\"",
      "StatusCode": 200,
      "ResponseTemplate": {
        "id": "{{path.id}}",
        "name": "Admin User (Mocked)",
        "email": "admin.mock@example.com",
        "role": "admin",
        "token_used": "{{header.X-Admin-Token}}",
        "uuid": "{{expr|> Faker.Random.Uuid()}}"
      }
    },
    {
      "Condition": "body.status == \"pending\"", // Example for POST/PUT
      "StatusCode": 202,
      "ResponseTemplate": {
        "message": "Request for user {{path.id}} with status 'pending' accepted.",
        "received_payload": "{{body}}" // Full request body
      }
    },
    {
      "Condition": "default", // Default response if no other conditions match
      "StatusCode": 404,
      "ResponseTemplate": {
        "error": "User not found",
        "id_searched": "{{path.id}}"
      }
    }
  ]
}
```

- **`Endpoint`**: The URL path for the mock. Supports path parameters like `/users/:id` or `/users/{id}`.
- **`Responses`**: An array of conditional response objects. They are evaluated in order.
    - **`Condition`**: A C#-like expression to determine if this response should be used.
        - Access request data:
            - `path.paramName` (e.g., `path.id`)
            - `query.paramName` (e.g., `query.page`)
            - `headers.HeaderName` (e.g., `headers.Authorization`, case-insensitive)
            - `body.fieldName` (e.g., `body.username`, for JSON bodies)
        - `default` can be used for a default fallback response.
    - **`StatusCode`**: The HTTP status code to return.
    - **`Headers`**: An object of response headers.
    - **`ResponseTemplate`**: The body of the response. Can be a JSON object or a string.
        - **Template Variables**:
            - `{{path.paramName}}`: Value of a path parameter.
            - `{{query.paramName}}`: Value of a query parameter.
            - `{{headers.HeaderName}}`: Value of a request header (case-insensitive).
            - `{{body.fieldName}}`: Value of a field from the JSON request body.
            - `{{body}}`: The full raw request body (string).
            - `{{expr|> Faker.Random.Int(min,max)}}`: A random integer between min and max (inclusive). E.g., `{{expr|> Faker.Random.Int(1,100)}}`.
            - `{{expr|> Faker.Random.Uuid()}}`: A random UUID.
            - `{{expr|> Faker.Date.Recent()}}`: A recent date.
            - Any environment or project variable (e.g., `{{baseUrl}}`).

## Commands

Apify commands are run as `apify <command> [subcommand] [options]` if installed globally, or `dotnet run -- <command> [subcommand] [options]` if run from the project directory.

### `apify init`
Initializes a new API testing project in the current directory.

```bash
apify init [--force]
```
- `--force`: Overwrite existing `apify-config.json` and `.apify` directory if they exist.
- Prompts for project name, default environment name, and other environments to create.
- Creates `apify-config.json`, `.apify/` directory with sample test and mock files.

### `apify create:request`
Interactively creates a new API test definition file.

```bash
apify create:request <file> [--force]
```
- `<file>`: (Required) The file path for the new API request definition (e.g., `users.all` becomes `.apify/users/all.json`). The `.json` extension is added automatically.
- `--force`: Overwrite if the file already exists.
- Prompts for request name, HTTP method, URI, headers, payload, and basic assertions.

### `apify create:mock`
Interactively creates a new mock API definition file.

```bash
apify create:mock <file> [--force]
```
- `<file>`: (Required) The file path for the new mock API definition (e.g., `users.get` becomes `.apify/users/get.mock.json`). The `.mock.json` extension is added automatically.
- `--force`: Overwrite if the file already exists.
- Prompts for mock name, HTTP method, endpoint path, status code, content type, response body, headers, and conditional responses.

### `apify call`
Executes an API test from a specified definition file.

```bash
apify call <file> [--env <environment_name>] [--verbose]
```
- `<file>`: (Required) An API definition file path (e.g., `users/all.json`). Dot notation like `users.all` is also supported.
- `--env <environment_name>`: Specifies the environment to use (e.g., "Production"). Uses default from `apify-config.json` if not set.
- `--verbose` or `-v`: Displays detailed output, including request and response bodies.

### `apify tests`
Runs all API tests found in the `.apify` directory and its subdirectories.

```bash
apify tests [--env <environment_name>] [--tag <tag_name>] [--verbose]
```
- `--env <environment_name>`: Specifies the environment.
- `--tag <tag_name>`: Filters and runs only tests that have the specified tag in their definition.
- `--verbose` or `-v`: Displays detailed output.
- Shows visual progress indicators and a summary at the end.

### `apify server:mock`
Starts a local API mock server using mock definition files.

```bash
apify server:mock [--port <port_number>] [--directory <path_to_mocks>] [--verbose]
```
- `--port <port_number>`: Port for the mock server (default: from `apify-config.json` or 1988).
- `--directory <path_to_mocks>`: Directory containing mock definition files (default: `.apify`).
- `--verbose` or `-v`: Enable verbose logging for the mock server.
- Reads configuration from the `MockServer` block in `apify-config.json` but command-line options take precedence.

### `apify list-env`
Lists all available environments and their variables from `apify-config.json`.

```bash
apify list-env
```

### Global Options
These options can be used with most commands.

- `--debug`: Show detailed debug output, including stack traces and internal logging. Useful for troubleshooting Apify itself.

## CI/CD with GitHub Actions

This project includes GitHub Actions workflows for continuous integration and releases.

### Continuous Integration

The CI workflow (if included in your project, typically in `.github/workflows/ci.yml`) runs on every push to the main branch and pull requests. It usually:
1. Builds the project.
2. Runs API tests (e.g., `apify tests --env ci`) to verify functionality against a deployed or mocked environment.

### Release Process

To create a release (if configured, typically in `.github/workflows/release.yml`):
1. **Tag-based release**: Create and push a new tag: `git tag -a v1.0.0 -m "Version 1.0.0" && git push origin v1.0.0`
2. **Manual release**: Via GitHub Actions UI.

This process typically builds single file executables for various platforms and attaches them to a GitHub release.

## Examples

### Example 1: Testing and Mocking a User API

1.  **Initialize Project:**
    ```bash
    apify init
    ```

2.  **Create a Mock for `GET /api/users/:id`:**
    Use `apify create:mock users.getById` and define it like this in `.apify/users/getById.mock.json`:
    ```json
    {
      "Name": "Mock User by ID",
      "Method": "GET",
      "Endpoint": "/api/users/:id",
      "Responses": [
        {
          "Condition": "path.id == \"1\"",
          "StatusCode": 200,
          "ResponseTemplate": { "id": 1, "name": "Mocked Alice", "email": "alice.mock@example.com" }
        },
        {
          "Condition": "default",
          "StatusCode": 404,
          "ResponseTemplate": { "error": "MockUser not found" }
        }
      ]
    }
    ```

3.  **Start the Mock Server:**
    ```bash
    apify server:mock --port 8080
    ```
    (In a separate terminal)

4.  **Create an API Test for `GET /api/users/1`:**
    Use `apify create:request users.getUser1` and define it in `.apify/users/getUser1.json`:
    ```json
    {
      "Name": "Get User 1",
      "Url": "{{baseUrl}}/users/1", // baseUrl will be http://localhost:8080/api
      "Method": "GET",
      "Tests": [
        { "Title": "Status 200", "Case": "Assert.Response.StatusCodeIs(200)" },
        { "Title": "ID is 1", "Case": "Assert.Equals(1, Response.Json.id)" },
        { "Title": "Name is Mocked Alice", "Case": "Assert.Equals(\"Mocked Alice\", Response.Json.name)" }
      ]
    }
    ```

5.  **Run the Test:**
    ```bash
    apify call users.getUser1 --verbose
    # This will hit your running mock server.
    ```

## License

This project is licensed under the MIT License.